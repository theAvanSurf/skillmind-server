namespace SkillMind.Core.Domain.Certificates;

/// <summary>
/// Server-side safe HTML templates. Keys are the only values accepted from clients —
/// raw HTML is never accepted as input, eliminating stored-XSS risk.
/// Placeholders: {{studentName}}, {{courseName}}, {{date}}, {{uniqueCode}}, {{logoUrl}}, {{signatureUrl}}
/// </summary>
public static class PredefinedCertificateTemplates
{
    public const string Classic = "classic";
    public const string Modern  = "modern";
    public const string Minimal = "minimal";

    public static readonly IReadOnlySet<string> ValidKeys =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Classic, Modern, Minimal };

    public static string Resolve(string key) => key.ToLowerInvariant() switch
    {
        Classic  => ClassicHtml,
        Modern   => ModernHtml,
        Minimal  => MinimalHtml,
        _        => ClassicHtml,
    };

    // ── Classic ───────────────────────────────────────────────────────────────
    private const string ClassicHtml = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <style>
            @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:ital,wght@0,400;0,700;1,400&family=Inter:wght@300;400;600&display=swap');
            * { margin:0; padding:0; box-sizing:border-box; }
            body { width:1000px; height:707px; background:#fff; font-family:'Inter',sans-serif; overflow:hidden; }
            .border { position:absolute; inset:18px; border:3px solid #c9a84c; pointer-events:none; }
            .inner-border { position:absolute; inset:24px; border:1px solid #c9a84c; pointer-events:none; }
            .content { position:relative; width:100%; height:100%; display:flex; flex-direction:column; align-items:center; justify-content:center; padding:48px; gap:20px; text-align:center; }
            .logo { max-height:56px; object-fit:contain; }
            .label { font-size:12px; letter-spacing:0.25em; text-transform:uppercase; color:#888; }
            .title { font-family:'Playfair Display',serif; font-size:44px; color:#1a1a1a; }
            .student { font-family:'Playfair Display',serif; font-size:32px; font-style:italic; color:#c9a84c; border-bottom:1px solid #c9a84c; padding-bottom:6px; }
            .course { font-size:18px; color:#333; max-width:600px; line-height:1.5; }
            .course strong { font-weight:600; }
            .meta { display:flex; gap:64px; margin-top:16px; }
            .meta-item { display:flex; flex-direction:column; align-items:center; gap:4px; }
            .meta-item span { font-size:11px; color:#aaa; letter-spacing:0.1em; text-transform:uppercase; }
            .meta-item strong { font-size:13px; color:#555; font-family:'Inter',sans-serif; }
            .signature { max-height:44px; object-fit:contain; }
          </style>
        </head>
        <body>
          <div class="border"></div>
          <div class="inner-border"></div>
          <div class="content">
            {{#if logoUrl}}<img class="logo" src="{{logoUrl}}" alt="logo" />{{/if}}
            <p class="label">Certificate of Completion</p>
            <h1 class="title">SkillMind</h1>
            <p class="label">This certifies that</p>
            <p class="student">{{studentName}}</p>
            <p class="course">has successfully completed <strong>{{courseName}}</strong></p>
            <div class="meta">
              <div class="meta-item"><span>Date</span><strong>{{date}}</strong></div>
              <div class="meta-item"><span>Certificate ID</span><strong>{{uniqueCode}}</strong></div>
            </div>
            {{#if signatureUrl}}<img class="signature" src="{{signatureUrl}}" alt="signature" />{{/if}}
          </div>
        </body>
        </html>
        """;

    // ── Modern ────────────────────────────────────────────────────────────────
    private const string ModernHtml = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <style>
            @import url('https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@300;400;600;700&display=swap');
            * { margin:0; padding:0; box-sizing:border-box; }
            body { width:1000px; height:707px; background:#0f0f1a; font-family:'Space Grotesk',sans-serif; overflow:hidden; color:#fff; }
            .accent { position:absolute; top:0; left:0; width:320px; height:100%; background:linear-gradient(135deg,#3b5bdb,#7048e8); clip-path:polygon(0 0,80% 0,60% 100%,0 100%); }
            .content { position:relative; width:100%; height:100%; display:flex; flex-direction:column; justify-content:center; padding:56px 64px 56px 380px; gap:16px; }
            .logo { max-height:44px; object-fit:contain; margin-bottom:8px; filter:brightness(0) invert(1); }
            .label { font-size:11px; letter-spacing:0.3em; text-transform:uppercase; color:#7c7caa; }
            .student { font-size:38px; font-weight:700; color:#fff; line-height:1.1; }
            .course { font-size:16px; color:#aaa; line-height:1.6; max-width:480px; }
            .course strong { color:#fff; }
            .divider { width:48px; height:3px; background:linear-gradient(90deg,#3b5bdb,#7048e8); border-radius:2px; margin:4px 0; }
            .meta { display:flex; gap:48px; margin-top:12px; }
            .meta-item span { font-size:10px; color:#555; text-transform:uppercase; letter-spacing:0.12em; display:block; }
            .meta-item strong { font-size:13px; color:#aaa; }
            .signature { max-height:40px; object-fit:contain; margin-top:8px; filter:brightness(0) invert(1); opacity:0.7; }
            .left-content { position:absolute; left:0; top:0; width:320px; height:100%; display:flex; flex-direction:column; align-items:center; justify-content:center; gap:12px; z-index:1; padding:32px; }
            .badge { width:80px; height:80px; border-radius:50%; background:rgba(255,255,255,0.15); border:2px solid rgba(255,255,255,0.3); display:flex; align-items:center; justify-content:center; font-size:32px; }
            .left-label { font-size:10px; letter-spacing:0.3em; text-transform:uppercase; color:rgba(255,255,255,0.6); text-align:center; }
          </style>
        </head>
        <body>
          <div class="accent"></div>
          <div class="left-content">
            {{#if logoUrl}}<img class="logo" src="{{logoUrl}}" alt="logo" />{{else}}<div class="badge">🎓</div>{{/if}}
            <p class="left-label">Certificate of<br/>Completion</p>
          </div>
          <div class="content">
            <p class="label">Awarded to</p>
            <h1 class="student">{{studentName}}</h1>
            <div class="divider"></div>
            <p class="course">for successfully completing<br/><strong>{{courseName}}</strong></p>
            <div class="meta">
              <div class="meta-item"><span>Date</span><strong>{{date}}</strong></div>
              <div class="meta-item"><span>ID</span><strong>{{uniqueCode}}</strong></div>
            </div>
            {{#if signatureUrl}}<img class="signature" src="{{signatureUrl}}" alt="signature" />{{/if}}
          </div>
        </body>
        </html>
        """;

    // ── Minimal ───────────────────────────────────────────────────────────────
    private const string MinimalHtml = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <style>
            @import url('https://fonts.googleapis.com/css2?family=DM+Serif+Display:ital@0;1&family=DM+Sans:wght@300;400;500&display=swap');
            * { margin:0; padding:0; box-sizing:border-box; }
            body { width:1000px; height:707px; background:#fafaf8; font-family:'DM Sans',sans-serif; overflow:hidden; }
            .top-bar { height:6px; background:#1a1a1a; }
            .content { width:100%; height:calc(100% - 6px); display:flex; flex-direction:column; align-items:center; justify-content:center; padding:48px; gap:18px; text-align:center; }
            .logo { max-height:40px; object-fit:contain; }
            .label { font-size:11px; letter-spacing:0.3em; text-transform:uppercase; color:#999; }
            .student { font-family:'DM Serif Display',serif; font-size:48px; color:#1a1a1a; line-height:1; }
            .sub { font-size:15px; color:#555; max-width:520px; line-height:1.7; }
            .sub strong { color:#1a1a1a; font-weight:500; }
            .line { width:64px; height:1px; background:#ddd; }
            .meta { display:flex; gap:56px; }
            .meta-item { display:flex; flex-direction:column; align-items:center; gap:3px; }
            .meta-item span { font-size:10px; color:#bbb; text-transform:uppercase; letter-spacing:0.15em; }
            .meta-item strong { font-size:13px; color:#666; }
            .signature { max-height:40px; object-fit:contain; opacity:0.75; }
          </style>
        </head>
        <body>
          <div class="top-bar"></div>
          <div class="content">
            {{#if logoUrl}}<img class="logo" src="{{logoUrl}}" alt="logo" />{{/if}}
            <p class="label">Certificate of Completion</p>
            <h1 class="student">{{studentName}}</h1>
            <p class="sub">has completed <strong>{{courseName}}</strong></p>
            <div class="line"></div>
            <div class="meta">
              <div class="meta-item"><span>Date</span><strong>{{date}}</strong></div>
              <div class="meta-item"><span>Cert. ID</span><strong>{{uniqueCode}}</strong></div>
            </div>
            {{#if signatureUrl}}<img class="signature" src="{{signatureUrl}}" alt="signature" />{{/if}}
          </div>
        </body>
        </html>
        """;
}
