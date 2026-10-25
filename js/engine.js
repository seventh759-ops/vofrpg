'use strict';
// エンジン: 入力 / サウンド / 描画ヘルパー / シーン管理 / 汎用UI

const W = 640, H = 480, TILE = 32;
const canvas = document.getElementById('game');
const ctx = canvas.getContext('2d');
ctx.imageSmoothingEnabled = false;
const FONT = "'DotGothic16', 'Hiragino Kaku Gothic ProN', Meiryo, sans-serif";

const rand = (a, b) => a + Math.floor(Math.random() * (b - a + 1));
const pick = arr => arr[Math.floor(Math.random() * arr.length)];
const clamp = (v, a, b) => Math.max(a, Math.min(b, v));

// ---------- 入力 ----------
const KEYMAP = {
  ArrowUp: 'up', w: 'up', W: 'up', ArrowDown: 'down', s: 'down', S: 'down',
  ArrowLeft: 'left', a: 'left', A: 'left', ArrowRight: 'right', d: 'right', D: 'right',
  z: 'ok', Z: 'ok', Enter: 'ok', ' ': 'ok',
  x: 'cancel', X: 'cancel', Escape: 'cancel', Backspace: 'cancel',
};

const Input = {
  held: {}, pressed: {}, holdT: {},
  down(k) { if (!this.held[k]) this.pressed[k] = true; this.held[k] = true; Sound.unlock(); },
  up(k) { this.held[k] = false; },
  // 押した瞬間 + 長押しでリピート
  rep(k) {
    if (this.pressed[k]) return true;
    const t = this.holdT[k] || 0;
    return t > 18 && t % 5 === 0;
  },
  endFrame() {
    this.pressed = {};
    for (const k in this.held) this.holdT[k] = this.held[k] ? (this.holdT[k] || 0) + 1 : 0;
  },
};

window.addEventListener('keydown', e => {
  const k = KEYMAP[e.key];
  if (!k) return;
  e.preventDefault();
  if (!e.repeat) Input.down(k);
});
window.addEventListener('keyup', e => { const k = KEYMAP[e.key]; if (k) Input.up(k); });
window.addEventListener('blur', () => { Input.held = {}; });
document.querySelectorAll('[data-key]').forEach(el => {
  const k = el.dataset.key;
  el.addEventListener('pointerdown', e => { e.preventDefault(); Input.down(k); });
  ['pointerup', 'pointercancel', 'pointerleave'].forEach(ev => el.addEventListener(ev, () => Input.up(k)));
});

// ---------- サウンド (WebAudio の簡易効果音) ----------
const Sound = {
  ac: null,
  unlock() {
    if (this.ac) return;
    try { this.ac = new (window.AudioContext || window.webkitAudioContext)(); } catch (e) { this.ac = null; }
  },
  tone(freq, dur, opts = {}) {
    if (!this.ac) return;
    const { type = 'square', vol = 0.04, delay = 0, to = null } = opts;
    const t0 = this.ac.currentTime + delay;
    const o = this.ac.createOscillator(), g = this.ac.createGain();
    o.type = type;
    o.frequency.setValueAtTime(freq, t0);
    if (to) o.frequency.exponentialRampToValueAtTime(to, t0 + dur);
    g.gain.setValueAtTime(vol, t0);
    g.gain.exponentialRampToValueAtTime(0.0001, t0 + dur);
    o.connect(g); g.connect(this.ac.destination);
    o.start(t0); o.stop(t0 + dur + 0.02);
  },
  play(name) {
    if (!this.ac) return;
    const T = (f, d, o) => this.tone(f, d, o);
    switch (name) {
      case 'cursor': T(880, 0.03, { vol: 0.02 }); break;
      case 'ok': T(660, 0.05); T(990, 0.07, { delay: 0.05 }); break;
      case 'cancel': T(440, 0.06, { to: 300 }); break;
      case 'buzz': T(120, 0.12, { type: 'sawtooth' }); break;
      case 'hit': T(200, 0.1, { type: 'sawtooth', to: 60, vol: 0.06 }); break;
      case 'crit': T(300, 0.15, { type: 'sawtooth', to: 50, vol: 0.08 }); break;
      case 'magic': T(500, 0.25, { type: 'triangle', to: 1400, vol: 0.06 }); break;
      case 'heal': [523, 659, 784, 1046].forEach((f, i) => T(f, 0.12, { type: 'triangle', delay: i * 0.06 })); break;
      case 'kill': T(400, 0.2, { type: 'triangle', to: 80 }); break;
      case 'dragon': T(110, 0.6, { type: 'sawtooth', to: 440, vol: 0.07 }); T(220, 0.6, { type: 'square', to: 880, delay: 0.1 }); break;
      case 'encounter': [880, 660, 880, 660, 1100].forEach((f, i) => T(f, 0.06, { delay: i * 0.05 })); break;
      case 'victory': [523, 523, 523, 659, 784, 659, 784].forEach((f, i) => T(f, 0.14, { delay: i * 0.12 })); break;
      case 'join': [392, 523, 659, 784].forEach((f, i) => T(f, 0.15, { type: 'triangle', delay: i * 0.1 })); break;
      case 'chest': [659, 784, 988, 1318].forEach((f, i) => T(f, 0.1, { delay: i * 0.07 })); break;
      case 'bite': T(1200, 0.08); T(1200, 0.08, { delay: 0.12 }); break;
      case 'step': T(80, 0.03, { type: 'triangle', vol: 0.02 }); break;
      case 'smash': T(90, 0.35, { type: 'sawtooth', to: 30, vol: 0.09 }); break;
    }
  },
};

// ---------- スプライト / タイル ----------
const spriteCache = {};
function getSprite(name, white = false) {
  const key = name + (white ? '#w' : '');
  if (spriteCache[key]) return spriteCache[key];
  const def = SPRITES[name];
  const c = document.createElement('canvas');
  c.width = def.rows[0].length; c.height = def.rows.length;
  const g = c.getContext('2d');
  def.rows.forEach((row, y) => {
    for (let x = 0; x < row.length; x++) {
      const col = def.pal[row[x]];
      if (!col || row[x] === '.') continue;
      g.fillStyle = col;
      g.fillRect(x, y, 1, 1);
    }
  });
  if (white) {
    g.globalCompositeOperation = 'source-in';
    g.fillStyle = '#fff';
    g.fillRect(0, 0, c.width, c.height);
  }
  return (spriteCache[key] = c);
}

// 表示サイズ (ドット数 x 倍率)
function spriteSize(name, scale) {
  const img = getSprite(name);
  return { w: img.width * scale, h: img.height * scale };
}

function drawSprite(name, x, y, scale = 2, opts = {}) {
  const img = getSprite(name, !!opts.white);
  const w = img.width * scale, h = img.height * scale;
  ctx.save();
  ctx.imageSmoothingEnabled = false;
  if (opts.alpha != null) ctx.globalAlpha = opts.alpha;
  if (opts.flip) {
    ctx.translate(Math.round(x) + w, Math.round(y));
    ctx.scale(-1, 1);
    ctx.drawImage(img, 0, 0, w, h);
  } else {
    ctx.drawImage(img, Math.round(x), Math.round(y), w, h);
  }
  ctx.restore();
}

const tileCache = {};
function getTile(ch, f = 0) {
  const animated = ch === '~' || ch === '=';
  const key = ch + (animated ? f : '');
  if (tileCache[key]) return tileCache[key];
  const c = document.createElement('canvas');
  c.width = c.height = TILE;
  const g = c.getContext('2d');
  const R = (x, y, w, h, col) => { g.fillStyle = col; g.fillRect(x, y, w, h); };
  const circle = (x, y, r, col) => { g.fillStyle = col; g.beginPath(); g.arc(x, y, r, 0, Math.PI * 2); g.fill(); };
  const tri = (pts, col) => {
    g.fillStyle = col; g.beginPath(); g.moveTo(pts[0], pts[1]);
    for (let i = 2; i < pts.length; i += 2) g.lineTo(pts[i], pts[i + 1]);
    g.closePath(); g.fill();
  };
  const grass = () => {
    R(0, 0, 32, 32, '#5fae4c');
    [[4, 6], [20, 4], [12, 18], [26, 22], [6, 26]].forEach(([x, y]) => { R(x, y, 2, 2, '#4c9440'); R(x + 2, y - 2, 2, 2, '#4c9440'); });
    [[16, 12], [2, 14], [24, 28]].forEach(([x, y]) => R(x, y, 2, 2, '#7cc866'));
  };
  const floor = () => {
    R(0, 0, 32, 32, '#5c4a42');
    [[6, 8], [22, 14], [12, 24], [26, 4]].forEach(([x, y]) => R(x, y, 3, 2, '#4a3a34'));
    R(16, 6, 2, 2, '#6e5c52');
  };
  const water = () => {
    R(0, 0, 32, 32, '#3474d0');
    const o = f * 8;
    [[2, 6], [18, 14], [8, 24]].forEach(([x, y]) => { R((x + o) % 32, y, 8, 2, '#7ab4f4'); });
  };
  const mountain = () => {
    grass();
    tri([1, 30, 16, 3, 31, 30], '#7a6a58');
    tri([16, 3, 31, 30, 16, 30], '#5e5040');
    tri([16, 3, 11, 12, 21, 12], '#f4f4f8');
  };
  const wallBase = () => {
    R(0, 0, 32, 32, '#e6d4a8');
    R(0, 0, 32, 3, '#8a6a40'); R(0, 29, 32, 3, '#8a6a40'); R(0, 0, 3, 32, '#8a6a40');
  };
  switch (ch) {
    case '.': grass(); break;
    case ',': case 'E':
      R(0, 0, 32, 32, '#d6b478');
      [[6, 6], [20, 12], [10, 22], [24, 26]].forEach(([x, y]) => R(x, y, 3, 2, '#bf9c60'));
      break;
    case 'g':
      R(0, 0, 32, 32, '#3d8a38');
      [[4, 6], [20, 4], [12, 18], [26, 22]].forEach(([x, y]) => R(x, y, 2, 2, '#2f7030'));
      break;
    case 'T':
      grass(); R(13, 20, 6, 10, '#6a4022');
      circle(16, 13, 12, '#1e5a26'); circle(15, 12, 10, '#2f7d34'); circle(12, 9, 4, '#4aa048');
      break;
    case '~': water(); break;
    case '^': mountain(); break;
    case '*':
      R(0, 0, 32, 32, '#3d8a38');
      [[8, 10], [23, 9], [15, 22]].forEach(([x, y]) => {
        R(x - 1, y + 4, 3, 6, '#5a3a1c'); circle(x, y, 7, '#1f5e26'); circle(x - 2, y - 2, 3, '#3a8a3a');
      });
      break;
    case 'v':
      grass();
      tri([2, 16, 9, 8, 16, 16], '#c0402e'); R(4, 16, 10, 8, '#e6d4a8'); R(8, 19, 3, 5, '#6a3a1a');
      tri([15, 22, 23, 13, 31, 22], '#c0402e'); R(17, 22, 12, 8, '#e6d4a8'); R(22, 25, 3, 5, '#6a3a1a');
      break;
    case 'c':
      mountain();
      circle(16, 24, 7, '#120c10'); R(9, 24, 14, 7, '#120c10');
      break;
    case '=':
      water(); R(0, 4, 32, 24, '#a8743a');
      for (let x = 5; x < 32; x += 6) R(x, 4, 1, 24, '#7a4e22');
      R(0, 4, 32, 2, '#7a4e22'); R(0, 26, 32, 2, '#7a4e22');
      break;
    case 'H':
      R(0, 0, 32, 32, '#b4402e');
      for (let y = 0; y < 32; y += 8) {
        R(0, y + 6, 32, 2, '#86281c');
        for (let x = (y / 8) % 2 ? 4 : 0; x < 32; x += 8) R(x, y, 1, 6, '#9a3424');
      }
      break;
    case 'W':
      wallBase(); R(10, 9, 12, 10, '#5a8ad0'); R(15, 9, 2, 10, '#8a6a40'); R(10, 13, 12, 2, '#8a6a40');
      break;
    case 'D':
      wallBase(); R(8, 6, 16, 26, '#6a3a1a'); R(10, 8, 12, 24, '#84501f'); R(19, 18, 2, 2, '#f2d04b');
      break;
    case 'F':
      grass(); R(15, 14, 3, 16, '#6a4022'); R(5, 5, 22, 12, '#5a3a1c'); R(6, 6, 20, 10, '#b8844a');
      R(10, 10, 9, 2, '#3474d0'); R(19, 9, 2, 4, '#3474d0');
      break;
    case '#':
      R(0, 0, 32, 32, '#352c3c');
      R(0, 10, 32, 2, '#241c2a'); R(0, 21, 32, 2, '#241c2a');
      R(10, 0, 2, 10, '#241c2a'); R(22, 12, 2, 9, '#241c2a'); R(6, 23, 2, 9, '#241c2a');
      R(0, 0, 32, 2, '#4a4058');
      break;
    case 'f': floor(); break;
    case 'R':
      floor(); circle(16, 19, 13, '#2a2228'); circle(16, 18, 12, '#8a8a96'); circle(12, 13, 5, '#b0b0bc');
      R(18, 16, 6, 2, '#5a5a66'); R(22, 18, 2, 5, '#5a5a66');
      break;
    case '<':
      floor(); R(6, 6, 20, 6, '#a89a8c'); R(6, 14, 20, 6, '#8a7a6c'); R(6, 22, 20, 6, '#6a5a4c');
      break;
    default: R(0, 0, 32, 32, '#000');
  }
  return (tileCache[key] = c);
}

// ---------- テキスト / ウィンドウ ----------
function drawText(str, x, y, color = '#fff', size = 20, align = 'left') {
  ctx.font = `${size}px ${FONT}`;
  ctx.textAlign = align;
  ctx.textBaseline = 'top';
  ctx.fillStyle = 'rgba(0,0,0,0.85)';
  ctx.fillText(str, x + 2, y + 2);
  ctx.fillStyle = color;
  ctx.fillText(str, x, y);
}

function wrapText(str, maxW, size = 20) {
  ctx.font = `${size}px ${FONT}`;
  const out = [];
  for (const para of String(str).split('\n')) {
    let line = '';
    for (const ch of para) {
      if (ctx.measureText(line + ch).width > maxW && line) { out.push(line); line = ''; }
      line += ch;
    }
    out.push(line);
  }
  return out;
}

function roundRectPath(x, y, w, h, r) {
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.arcTo(x + w, y, x + w, y + h, r);
  ctx.arcTo(x + w, y + h, x, y + h, r);
  ctx.arcTo(x, y + h, x, y, r);
  ctx.arcTo(x, y, x + w, y, r);
  ctx.closePath();
}

function drawWindow(x, y, w, h) {
  const gr = ctx.createLinearGradient(0, y, 0, y + h);
  gr.addColorStop(0, '#3048a8');
  gr.addColorStop(1, '#141c58');
  ctx.save();
  roundRectPath(x, y, w, h, 6);
  ctx.fillStyle = gr;
  ctx.globalAlpha = 0.97;
  ctx.fill();
  ctx.globalAlpha = 1;
  ctx.lineWidth = 3;
  ctx.strokeStyle = '#eef0ff';
  ctx.stroke();
  roundRectPath(x + 4, y + 4, w - 8, h - 8, 4);
  ctx.lineWidth = 1;
  ctx.strokeStyle = '#7080c0';
  ctx.stroke();
  ctx.restore();
}

function drawBar(x, y, w, h, ratio, color) {
  ctx.fillStyle = '#0a0a20';
  ctx.fillRect(x, y, w, h);
  ctx.fillStyle = color;
  ctx.fillRect(x + 1, y + 1, Math.max(0, (w - 2) * clamp(ratio, 0, 1)), h - 2);
}

// ---------- メニュー ----------
// items: [{label, right, disabled, ...}]
class Menu {
  constructor(items, opts = {}) {
    this.items = items;
    this.i = 0;
    this.top = 0;
    this.rows = opts.rows || items.length || 1;
  }
  get cur() { return this.items[this.i]; }
  update() {
    const n = this.items.length;
    if (Input.pressed.cancel) { Sound.play('cancel'); return 'cancel'; }
    if (!n) return null;
    if (Input.rep('up')) { this.i = (this.i - 1 + n) % n; Sound.play('cursor'); }
    if (Input.rep('down')) { this.i = (this.i + 1) % n; Sound.play('cursor'); }
    if (Input.pressed.ok) {
      if (this.cur.disabled) { Sound.play('buzz'); return null; }
      Sound.play('ok');
      return 'ok';
    }
    return null;
  }
  draw(x, y, w, opts = {}) {
    const { lineH = 32, size = 20, active = true } = opts;
    const n = this.items.length;
    if (this.i < this.top) this.top = this.i;
    if (this.i >= this.top + this.rows) this.top = this.i - this.rows + 1;
    for (let r = 0; r < Math.min(this.rows, n - this.top); r++) {
      const idx = this.top + r;
      const it = this.items[idx];
      const yy = y + r * lineH;
      const col = it.disabled ? '#8088a8' : (it.color || '#fff');
      if (idx === this.i && (active || (Game.frame >> 3) % 2)) drawText('▶', x, yy, active ? '#ffe080' : '#a0a0c0', size);
      drawText(it.label, x + 24, yy, col, size);
      if (it.right != null) drawText(String(it.right), x + w, yy, col, size, 'right');
    }
    if (this.top > 0) drawText('▲', x + w / 2, y - 18, '#ccd', 14, 'center');
    if (this.top + this.rows < n) drawText('▼', x + w / 2, y + this.rows * lineH - 6, '#ccd', 14, 'center');
  }
}

// ---------- シーン管理 ----------
const Game = {
  scenes: [],
  frame: 0,
  push(s) { this.scenes.push(s); },
  pop() { return this.scenes.pop(); },
  remove(s) { const i = this.scenes.indexOf(s); if (i >= 0) this.scenes.splice(i, 1); },
  replace(...ss) { this.scenes = ss; },
  top() { return this.scenes[this.scenes.length - 1]; },
};

// フェードアウト → mid() → フェードイン。自分は常に最前面に残る
class Fade {
  constructor(mid, dur = 16) { this.t = 0; this.mid = mid; this.dur = dur; }
  update() {
    this.t++;
    if (this.t === this.dur) {
      if (this.mid) this.mid();
      Game.remove(this);
      Game.push(this);
    }
    if (this.t >= this.dur * 2) Game.remove(this);
  }
  draw() {
    const a = this.t < this.dur ? this.t / this.dur : 1 - (this.t - this.dur) / this.dur;
    ctx.fillStyle = `rgba(0,0,0,${clamp(a, 0, 1)})`;
    ctx.fillRect(0, 0, W, H);
  }
}

// 会話ウィンドウ。"話者|本文" 形式に対応。choices があれば最後に選択肢
class Dialog {
  constructor(lines, onDone, choices) {
    this.lines = lines;
    this.li = 0;
    this.chars = 0;
    this.onDone = onDone;
    this.choices = choices;
    this.menu = null;
  }
  parse() {
    const line = this.lines[this.li];
    const p = line.indexOf('|');
    return p >= 0 ? [line.slice(0, p), line.slice(p + 1)] : [null, line];
  }
  finish(arg) {
    Game.remove(this);
    if (this.onDone) this.onDone(arg);
  }
  update() {
    if (this.menu) {
      const r = this.menu.update();
      if (r === 'ok') this.finish(this.menu.i);
      else if (r === 'cancel') this.finish(this.choices.length - 1);
      return;
    }
    const txt = this.parse()[1];
    if (this.chars < txt.length) {
      this.chars += 1.5;
      if (Input.pressed.ok) this.chars = txt.length;
      return;
    }
    if (this.choices && this.li === this.lines.length - 1) {
      this.menu = new Menu(this.choices.map(label => ({ label })));
      return;
    }
    if (Input.pressed.ok) {
      this.li++;
      this.chars = 0;
      if (this.li >= this.lines.length) { this.li--; this.finish(); }
    }
  }
  draw() {
    const [sp, txt] = this.parse();
    drawWindow(16, 336, 608, 128);
    if (sp) {
      ctx.font = `20px ${FONT}`;
      const w = ctx.measureText(sp).width + 32;
      drawWindow(24, 298, w, 40);
      drawText(sp, 40, 308, '#ffe080', 20);
    }
    wrapText(txt.slice(0, Math.floor(this.chars)), 568, 22)
      .forEach((l, i) => drawText(l, 36, 354 + i * 30, '#fff', 22));
    if (this.chars >= txt.length && !this.menu && (Game.frame >> 4) % 2) drawText('▼', 600, 438, '#fff', 16);
    if (this.menu) {
      drawWindow(452, 316 - this.choices.length * 34 - 20, 172, this.choices.length * 34 + 20);
      this.menu.draw(468, 316 - this.choices.length * 34 - 8, 140, { lineH: 34 });
    }
  }
}

function say(lines, onDone, choices) {
  Game.push(new Dialog(Array.isArray(lines) ? lines : [lines], onDone, choices));
}

// ---------- メインループ (60fps 固定ステップ) ----------
function startLoop() {
  let last = performance.now(), acc = 0;
  const STEP = 1000 / 60;
  function loop(now) {
    acc += Math.min(100, now - last);
    last = now;
    while (acc >= STEP) {
      acc -= STEP;
      Game.frame++;
      const t = Game.top();
      if (t) t.update();
      Input.endFrame();
    }
    ctx.fillStyle = '#000';
    ctx.fillRect(0, 0, W, H);
    for (const s of Game.scenes) s.draw();
    requestAnimationFrame(loop);
  }
  requestAnimationFrame(loop);
}
