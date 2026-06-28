/**
 * Note: 
 * 1. Add the secret binding in the Cloudflare worker.
 */

export default {
  async fetch(request, env, ctx) {
    if (request.method !== "POST") {
      return new Response("Method Not Allowed", { status: 405 });
    }

    const mySecret = env.Bug_Reporter_Token; 
    if (!mySecret) {
      return new Response("Secret 'Bug_Reporter_Token' is missing or binding failed.", { status: 500 });
    }

    try {
      const body = await request.json();
      const { issueTitle, userEmail, bugDescription, appVersion } = body;
      const issueBody = `### Description:\n${bugDescription}\n\n**Submitted By:** ${userEmail}\n**App Version:** ${appVersion}\n\n*Generated securely via Cloudflare proxy.*`;

      // Payload expected by GitHub API
      const githubPayload = {
        title: `🚨 ${issueTitle}`,
        body: issueBody,
        labels: ["bug", "USER REPORTED"]
      };

      const token = await env.Bug_Reporter_Token.get()
      const githubResponse = await fetch("https://api.github.com/repos/bee0018/Chasma/issues", {
        method: "POST",
        headers: {
          "Authorization": `Bearer ${token}`,
          "Accept": "application/vnd.github+json",
          "X-GitHub-Api-Version": "2022-11-28",
          "User-Agent": "Cloudflare-Bug-Reporter-Worker"
        },
        body: JSON.stringify(githubPayload)
      });

      if (!githubResponse.ok) {
        const errorText = await githubResponse.text();
        return new Response(`GitHub Error: ${errorText}`, { status: githubResponse.status });
      }

      return new Response(JSON.stringify({ success: true }), {
        status: 200,
        headers: { "Content-Type": "application/json" }
      });

    } catch (err) {
      return new Response(`Server Error: ${err.message}`, { status: 500 });
    }
  }
};