---
wave: 1
---

# Phase 3 Fix Plan: Syntax Highlighting

## Diagnosis
The syntax highlighting issue is caused by Blazor 8's enhanced navigation (`blazor.web.js`). When navigating between pages, Blazor replaces the DOM content but does not execute inline `<script>` tags injected by components like `RepoBlob.razor`. As a result, `hljs.highlightElement` is never called.

## Fix
1. Add a global event listener for Blazor's `enhancedload` event in `App.razor` to automatically trigger `highlight.js` on all `<pre><code>` blocks after navigation.
2. Call `hljs.highlightAll()` on initial page load as well.
3. Remove the now-redundant inline script from `RepoBlob.razor`.

## Wave 1: Apply Fixes

```xml
<task>
  <id>fix-highlighting</id>
  <description>Fix highlight.js with Blazor Enhanced Navigation</description>
  <action>
    - Edit `Aristokeides.Api/Components/App.razor`. After the `highlight.min.js` and `blazor.web.js` script tags, add a new script block:
      ```javascript
      <script>
          document.addEventListener('DOMContentLoaded', () => {
              if (typeof hljs !== 'undefined') { hljs.highlightAll(); }
          });
          Blazor.addEventListener('enhancedload', () => {
              if (typeof hljs !== 'undefined') { hljs.highlightAll(); }
          });
      </script>
      ```
    - Edit `Aristokeides.Api/Components/Pages/RepoBlob.razor`. Remove the inline `<script>` block that attempts to call `hljs.highlightElement`.
  </action>
  <read_first>
    - Aristokeides.Api/Components/App.razor
    - Aristokeides.Api/Components/Pages/RepoBlob.razor
  </read_first>
  <acceptance_criteria>
    - Code highlighting works both on initial page load and after navigating to a file.
  </acceptance_criteria>
</task>
```
