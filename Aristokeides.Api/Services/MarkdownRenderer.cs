using Markdig;

namespace Aristokeides.Api.Services;

/// <summary>
/// 마크다운 텍스트를 HTML로 안전하게 렌더링하는 서비스입니다.
/// HTML 태그 파싱을 비활성화하여 XSS 공격을 방지합니다.
/// </summary>
public class MarkdownRenderer
{
    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .Build();

    /// <summary>
    /// 마크다운 입력을 안전한 HTML 문자열로 변환합니다.
    /// </summary>
    /// <param name="markdown">마크다운 원본 텍스트</param>
    /// <returns>안전하게 변환된 HTML 문자열</returns>
    public static string RenderHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        return Markdown.ToHtml(markdown, _pipeline);
    }
}
