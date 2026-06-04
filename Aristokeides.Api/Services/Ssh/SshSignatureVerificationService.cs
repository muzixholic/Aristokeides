using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Aristokeides.Api.Services.Ssh;

/// <summary>
/// Git 푸시 시 유입된 커밋들의 SSH 서명을 자동으로 검증하여 DB에 적재하는 오케스트레이션 서비스.
/// </summary>
public class SshSignatureVerificationService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SshSignatureVerificationService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task VerifyNewCommitsAsync(string repoPhysicalPath, string oldOid, string newOid, Guid repositoryId)
    {
        if (string.IsNullOrEmpty(newOid) || newOid == "0000000000000000000000000000000000000000")
        {
            return; // 삭제된 브랜치/태그 등은 검증 필요 없음
        }

        using var repo = new LibGit2Sharp.Repository(repoPhysicalPath);
        var startCommit = repo.Lookup<Commit>(newOid);
        if (startCommit == null)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. 검증 대상 커밋들 수집 (BFS 탐색)
        var queue = new Queue<Commit>();
        var visited = new HashSet<string>();
        var commitsToVerify = new List<Commit>();

        queue.Enqueue(startCommit);
        visited.Add(startCommit.Sha);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.Sha.Equals(oldOid, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // oldOid가 전부 0인 경우(신규 브랜치/태그 생성), DB에 이미 검증이 기록되어 있는 경우 탐색을 조기 종료할 수 있음
            if (oldOid == "0000000000000000000000000000000000000000")
            {
                bool isAlreadyRecorded = await db.CommitSignatures.AnyAsync(s => s.RepositoryId == repositoryId && s.CommitHash == current.Sha);
                if (isAlreadyRecorded)
                {
                    continue; // 이 조상은 이미 이전 푸시 등에서 서명이 처리되었으므로 패스
                }
            }

            commitsToVerify.Add(current);

            foreach (var parent in current.Parents)
            {
                if (!visited.Contains(parent.Sha))
                {
                    visited.Add(parent.Sha);
                    queue.Enqueue(parent);
                }
            }
        }

        // 2. 수집된 커밋들에 대해 순차적으로 서명 검증 및 DB Upsert
        foreach (var commit in commitsToVerify)
        {
            try
            {
                var (payloadBytes, signatureText) = ExtractSignatureAndPayload(repoPhysicalPath, commit.Sha);

                string status;
                int? signerUserId = null;
                string? algorithm = null;
                string? fingerprint = null;

                if (string.IsNullOrEmpty(signatureText))
                {
                    status = "NoSignature";
                }
                else
                {
                    // 서명 데이터 파싱
                    var parsed = SshSignatureParser.ParseSignature(signatureText);
                    algorithm = parsed.algorithm;
                    fingerprint = parsed.fingerprint;
                    byte[] publicKeyBytes = parsed.publicKeyBytes;

                    // DB에서 일치하는 SSH 키 검색
                    var dbKey = await db.SshKeys
                        .FirstOrDefaultAsync(k => k.Fingerprint == fingerprint);

                    if (dbKey != null)
                    {
                        // 등록된 키가 있는 경우 검증
                        bool isValid = SshSignatureVerifier.Verify(payloadBytes, signatureText, dbKey.PublicKey);
                        status = isValid ? "Verified" : "Invalid";
                        if (isValid)
                        {
                            signerUserId = dbKey.UserId;
                        }
                    }
                    else
                    {
                        // 미등록 키의 경우 원시 공개키를 구성하여 무결성만 검증
                        string rawPublicKey = $"{algorithm} {Convert.ToBase64String(publicKeyBytes)}";
                        bool isValid = SshSignatureVerifier.Verify(payloadBytes, signatureText, rawPublicKey);
                        status = isValid ? "Unknown" : "Invalid";
                    }
                }

                // DB Upsert
                var existing = await db.CommitSignatures
                    .FirstOrDefaultAsync(s => s.RepositoryId == repositoryId && s.CommitHash == commit.Sha);

                if (existing != null)
                {
                    existing.Status = status;
                    existing.SignerUserId = signerUserId;
                    existing.Algorithm = algorithm;
                    existing.KeyFingerprint = fingerprint;
                    existing.VerifiedAt = DateTime.UtcNow;
                }
                else
                {
                    db.CommitSignatures.Add(new CommitSignature
                    {
                        RepositoryId = repositoryId,
                        CommitHash = commit.Sha,
                        Status = status,
                        SignerUserId = signerUserId,
                        Algorithm = algorithm,
                        KeyFingerprint = fingerprint,
                        VerifiedAt = DateTime.UtcNow
                    });
                }

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying signature for commit {commit.Sha}: {ex.Message}");
            }
        }
    }

    private (byte[] payloadBytes, string? signatureText) ExtractSignatureAndPayload(string repoPath, string commitHash)
    {
        using (var process = new Process())
        {
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"-C \"{repoPath}\" cat-file commit {commitHash}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Failed to retrieve commit raw data: {process.StandardError.ReadToEnd()}");
            }

            var lines = output.Split(new[] { "\n" }, StringSplitOptions.None);
            var signatureSb = new StringBuilder();
            var payloadLines = new List<string>();
            bool insideSignature = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedEndLine = line.EndsWith("\r") ? line.Substring(0, line.Length - 1) : line;

                if (trimmedEndLine.StartsWith("gpgsig "))
                {
                    insideSignature = true;
                    signatureSb.AppendLine(trimmedEndLine.Substring(7));
                    continue;
                }

                if (insideSignature)
                {
                    if (trimmedEndLine.StartsWith(" "))
                    {
                        signatureSb.AppendLine(trimmedEndLine.Substring(1));
                        continue;
                    }
                    else
                    {
                        insideSignature = false;
                    }
                }

                payloadLines.Add(line);
            }

            string? signature = signatureSb.Length > 0 ? signatureSb.ToString().Trim() : null;
            string payloadText = string.Join("\n", payloadLines);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadText);

            return (payloadBytes, signature);
        }
    }
}
