'use strict';
// クォータービュー描画ヘルパー
// マス (gx, gy) の上頂点 = 原点 + ((gx - gy) * 32, (gx + gy) * 16)。ひし形は 64x32。

// 背の高い地形の下に敷く地面
const GROUND_OF = { T: '.', F: '.', '^': '.', v: '.', c: '.', H: '.', W: '.', D: '.', '#': 'f', R: 'f', '*': 'g' };
const ISO_TALL = new Set(['T', 'F', '^', 'v', 'c', 'H', 'W', 'D', '#', 'R', '*']);

function polyFill(pts, color) {
  ctx.fillStyle = color;
  ctx.beginPath();
  ctx.moveTo(pts[0][0], pts[0][1]);
  for (let i = 1; i < pts.length; i++) ctx.lineTo(pts[i][0], pts[i][1]);
  ctx.closePath();
  ctx.fill();
}

function ellipse(x, y, rx, ry, color) {
  ctx.fillStyle = color;
  ctx.beginPath();
  ctx.ellipse(x, y, rx, ry, 0, 0, Math.PI * 2);
  ctx.fill();
}

// 平らな 32x32 のテクスチャをひし形に貼る (sx, sy = 上頂点)
function drawIsoGround(ch, sx, sy, f = 0) {
  if (ch === ' ') return;
  const img = getTile(GROUND_OF[ch] || ch, f);
  ctx.setTransform(1, 0.5, -1, 0.5, sx, sy);
  ctx.drawImage(img, 0, 0, 32.6, 32.6);
  ctx.setTransform(1, 0, 0, 1, 0, 0);
}

// 壁面などのテクスチャ
const faceCache = {};
function faceTex(name) {
  if (faceCache[name]) return faceCache[name];
  if (name === 'roof') return (faceCache[name] = getTile('H'));
  if (name === 'cave') return (faceCache[name] = getTile('#'));
  const c = document.createElement('canvas');
  c.width = c.height = 32;
  const g = c.getContext('2d');
  const R = (x, y, w, h, col) => { g.fillStyle = col; g.fillRect(x, y, w, h); };
  switch (name) {
    case 'wall': case 'wallplain': case 'door':
      R(0, 0, 32, 32, '#e6d4a8');
      R(0, 0, 32, 3, '#8a6a40'); R(0, 29, 32, 3, '#8a6a40'); R(0, 0, 3, 32, '#8a6a40');
      if (name === 'wall') { R(10, 9, 12, 10, '#5a8ad0'); R(15, 9, 2, 10, '#8a6a40'); R(10, 13, 12, 2, '#8a6a40'); }
      if (name === 'door') { R(9, 8, 14, 24, '#6a3a1a'); R(11, 10, 10, 22, '#84501f'); R(18, 19, 2, 2, '#f2d04b'); }
      break;
    case 'cavetop':
      R(0, 0, 32, 32, '#2a2230');
      [[5, 6], [20, 10], [12, 22], [26, 26]].forEach(([x, y]) => R(x, y, 3, 2, '#3a3040'));
      break;
  }
  return (faceCache[name] = c);
}

function linGrad(x0, y0, x1, y1, stops) {
  const gr = ctx.createLinearGradient(x0, y0, x1, y1);
  stops.forEach(([t, c]) => gr.addColorStop(t, c));
  return gr;
}

// 左上から光が当たった球 (木の葉や岩の丸み)
function ball(cx, cy, rx, ry, light, base, dark) {
  const gr = ctx.createRadialGradient(cx - rx * 0.4, cy - ry * 0.45, 1, cx, cy, Math.max(rx, ry) * 1.05);
  gr.addColorStop(0, light);
  gr.addColorStop(0.55, base);
  gr.addColorStop(1, dark);
  ellipse(cx, cy, rx, ry, gr);
}

// 直方体 (sx, sy = 地面のひし形の上頂点, h = 高さ)
function drawBlock(sx, sy, h, top, left, right) {
  // 右下の地面へ落ちる影
  const L = h / 56;
  const v = [32 * L, 16 * L];
  polyFill([[sx, sy], [sx + v[0], sy + v[1]], [sx + 32 + v[0], sy + 16 + v[1]], [sx + v[0], sy + 32 + v[1]],
            [sx, sy + 32], [sx - 32, sy + 16]], 'rgba(0,0,20,0.22)');
  ctx.setTransform(1, 0.5, 0, h / 32, sx - 32, sy + 16 - h);
  ctx.drawImage(left, 0, 0, 32.5, 32);
  ctx.setTransform(1, -0.5, 0, h / 32, sx, sy + 32 - h);
  ctx.drawImage(right, 0, 0, 32.5, 32);
  ctx.setTransform(1, 0, 0, 1, 0, 0);
  // 壁面は下ほど暗く (接地部分の影)、右面は全体を暗く
  polyFill([[sx - 32, sy + 16 - h], [sx, sy + 32 - h], [sx, sy + 32], [sx - 32, sy + 16]],
    linGrad(0, sy - h, 0, sy + 32, [[0, 'rgba(255,240,210,0.10)'], [0.6, 'rgba(0,0,0,0.08)'], [1, 'rgba(0,0,20,0.42)']]));
  polyFill([[sx, sy + 32 - h], [sx + 32, sy + 16 - h], [sx + 32, sy + 16], [sx, sy + 32]],
    linGrad(0, sy - h, 0, sy + 32, [[0, 'rgba(0,0,30,0.22)'], [0.6, 'rgba(0,0,30,0.36)'], [1, 'rgba(0,0,30,0.58)']]));
  ctx.setTransform(1, 0.5, -1, 0.5, sx, sy - h);
  ctx.drawImage(top, 0, 0, 32.6, 32.6);
  ctx.setTransform(1, 0, 0, 1, 0, 0);
  polyFill([[sx, sy - h], [sx + 32, sy + 16 - h], [sx, sy + 32 - h], [sx - 32, sy + 16 - h]],
    linGrad(sx - 16, sy - h, sx + 16, sy + 32 - h, [[0, 'rgba(255,250,230,0.22)'], [1, 'rgba(0,0,20,0.12)']]));
}

function drawIsoTree(cx, cy, k = 1) {
  ellipse(cx + 6 * k, cy + 1, 18 * k, 7 * k, 'rgba(0,0,20,0.28)');
  ctx.fillStyle = linGrad(cx - 3 * k, 0, cx + 3 * k, 0, [[0, '#8a5a32'], [1, '#4a2a14']]);
  ctx.fillRect(cx - 3 * k, cy - 20 * k, 6 * k, 20 * k);
  ball(cx, cy - 32 * k, 17 * k, 17 * k, '#3e8a3c', '#24682a', '#123a18');
  ball(cx - 3 * k, cy - 37 * k, 12 * k, 11 * k, '#6cc060', '#3a8c3c', '#246a2a');
  ball(cx - 7 * k, cy - 42 * k, 5 * k, 4 * k, '#a8e890', '#62b456', '#3a8c3c');
}

function drawIsoMountain(sx, sy, cave) {
  const apex = [sx, sy + 16 - 54], L = [sx - 32, sy + 16], B = [sx, sy + 32], Rt = [sx + 32, sy + 16];
  const lerp = (p, q, t) => [p[0] + (q[0] - p[0]) * t, p[1] + (q[1] - p[1]) * t];
  polyFill([[sx, sy], [sx + 50, sy + 25], [sx + 32, sy + 34], B, L], 'rgba(0,0,20,0.18)');
  polyFill([L, B, apex], linGrad(sx - 32, apex[1], sx, sy + 32, [[0, '#a8977c'], [1, '#6e5e4a']]));
  polyFill([B, Rt, apex], linGrad(sx, apex[1], sx + 32, sy + 32, [[0, '#6e5e4a'], [1, '#40362a']]));
  polyFill([lerp(apex, L, 0.3), lerp(apex, B, 0.3), apex], linGrad(sx - 10, apex[1], sx, apex[1] + 18, [[0, '#ffffff'], [1, '#dcdcec']]));
  polyFill([lerp(apex, B, 0.3), lerp(apex, Rt, 0.3), apex], linGrad(sx, apex[1], sx + 10, apex[1] + 18, [[0, '#c8c8d8'], [1, '#9898b0']]));
  if (cave) {
    ellipse(sx - 14, sy + 18, 9, 11, '#120c10');
    ctx.fillStyle = '#120c10';
    ctx.fillRect(sx - 23, sy + 18, 18, 6);
  }
}

function drawIsoRock(cx, cy) {
  ellipse(cx + 5, cy + 1, 20, 7, 'rgba(0,0,20,0.32)');
  ellipse(cx, cy - 12, 19, 15, '#2a2228');
  ball(cx, cy - 13, 17, 13, '#c4c4d0', '#8a8a96', '#4a4a56');
  ctx.fillStyle = '#5a5a66';
  ctx.fillRect(cx + 3, cy - 14, 6, 2);
}

function drawIsoSign(cx, cy) {
  ellipse(cx, cy, 8, 3, 'rgba(0,0,0,0.3)');
  ctx.fillStyle = '#6a4022'; ctx.fillRect(cx - 2, cy - 20, 4, 20);
  ctx.fillStyle = '#5a3a1c'; ctx.fillRect(cx - 13, cy - 34, 26, 15);
  ctx.fillStyle = '#b8844a'; ctx.fillRect(cx - 12, cy - 33, 24, 13);
  ctx.fillStyle = '#3474d0'; ctx.fillRect(cx - 7, cy - 28, 10, 2); ctx.fillRect(cx + 3, cy - 30, 2, 6);
}

function drawIsoHut(cx, cy, k = 1) {
  ctx.fillStyle = '#e6d4a8'; ctx.fillRect(cx - 9 * k, cy - 12 * k, 18 * k, 12 * k);
  polyFill([[cx - 12 * k, cy - 11 * k], [cx, cy - 22 * k], [cx + 12 * k, cy - 11 * k]], '#c0402e');
  ctx.fillStyle = '#6a3a1a'; ctx.fillRect(cx - 2 * k, cy - 7 * k, 4 * k, 7 * k);
}

function drawIsoChest(cx, cy, open) {
  ellipse(cx, cy, 14, 5, 'rgba(0,0,0,0.3)');
  ctx.fillStyle = '#1b1424'; ctx.fillRect(cx - 13, cy - 18, 26, 18);
  ctx.fillStyle = open ? '#5a3a1c' : linGrad(0, cy - 16, 0, cy - 2, [[0, '#d08a44'], [1, '#7a4418']]);
  ctx.fillRect(cx - 11, cy - 16, 22, 14);
  ctx.fillStyle = '#f2d04b'; ctx.fillRect(cx - 11, cy - 11, 22, 2); ctx.fillRect(cx - 2, cy - 13, 4, 6);
  if (open) { ctx.fillStyle = '#1b1424'; ctx.fillRect(cx - 11, cy - 16, 22, 4); }
}

// 背の高い地形 1 マス分 (sx, sy = 上頂点)
function drawIsoObject(ch, sx, sy) {
  const cx = sx, cy = sy + 16;
  switch (ch) {
    case 'T': drawIsoTree(cx, cy, 1.4); break;
    case '*':
      drawIsoTree(cx - 12, cy - 3, 0.55); drawIsoTree(cx + 12, cy - 3, 0.55); drawIsoTree(cx, cy + 6, 0.6);
      break;
    case '^': drawIsoMountain(sx, sy, false); break;
    case 'c': drawIsoMountain(sx, sy, true); break;
    case 'v': drawIsoHut(cx - 10, cy - 2); drawIsoHut(cx + 12, cy + 4, 0.85); break;
    case 'F': drawIsoSign(cx, cy); break;
    case 'R': drawIsoRock(cx, cy); break;
    case '#': drawBlock(sx, sy, 44, faceTex('cavetop'), faceTex('cave'), faceTex('cave')); break;
    case 'H': drawBlock(sx, sy, 72, faceTex('roof'), faceTex('wallplain'), faceTex('wallplain')); break;
    case 'W': drawBlock(sx, sy, 72, faceTex('roof'), faceTex('wall'), faceTex('wallplain')); break;
    case 'D': drawBlock(sx, sy, 72, faceTex('roof'), faceTex('door'), faceTex('wallplain')); break;
  }
}

// 足元に影を付けてキャラクターを描く (cx, cy = 足元)
function drawIsoChar(name, cx, cy, scale = 2, opts = {}) {
  const { w, h } = spriteSize(name, scale);
  ellipse(cx + w * 0.08, cy + 1, w * 0.36, w * 0.14, 'rgba(0,0,20,0.32)');
  drawSprite(name, cx - w / 2, cy - h + scale * 2 + (opts.lift || 0), scale, opts);
}
