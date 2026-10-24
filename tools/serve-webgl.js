// Tiny static server for the Unity WebGL build (no npm packages needed).
// Unity's *.gz files must be sent with "Content-Encoding: gzip" or the browser can't run them.
const http = require("http");
const fs = require("fs");
const path = require("path");

const root = path.resolve(__dirname, "..", "UnityProject", "Builds", "WebGL");
const port = process.env.PORT || 8080;
const types = {
  ".html": "text/html", ".js": "application/javascript", ".wasm": "application/wasm",
  ".data": "application/octet-stream", ".css": "text/css", ".png": "image/png",
  ".jpg": "image/jpeg", ".ico": "image/x-icon", ".json": "application/json",
};

http.createServer((req, res) => {
  let rel = decodeURIComponent(req.url.split("?")[0]);
  if (rel.endsWith("/")) rel += "index.html";
  const file = path.join(root, rel);
  if (!file.startsWith(root) || !fs.existsSync(file) || fs.statSync(file).isDirectory()) {
    res.writeHead(404); return res.end("Not found");
  }
  const headers = {};
  let name = file;
  if (name.endsWith(".gz")) { headers["Content-Encoding"] = "gzip"; name = name.slice(0, -3); }
  headers["Content-Type"] = types[path.extname(name)] || "application/octet-stream";
  res.writeHead(200, headers);
  fs.createReadStream(file).pipe(res);
}).listen(port, () => console.log(`Campus Nav 3D running at http://localhost:${port}  (Ctrl+C to stop)`));
