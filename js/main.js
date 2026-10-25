'use strict';
// ゲーム状態 / フィールド / イベント / 各種メニュー

let G = null;
const SAVE_KEY = 'ryuketsu_save_v1';

// ---------- キャラクター ----------
function stat(c, k) {
  const d = CHARS[c.id];
  let v = d.base[k] + Math.floor(d.grow[k] * (c.lv - 1));
  for (const slot of ['weapon', 'armor']) {
    const e = c[slot] && EQUIPS[c[slot]];
    if (e && e.bonus[k]) v += e.bonus[k];
  }
  return v;
}
const maxAp = c => (CHARS[c.id].dragon ? 20 + 3 * (c.lv - 1) : 0);
const spellsOf = c => CHARS[c.id].learn.filter(([lv]) => c.lv >= lv).map(([, s]) => s);
const expFor = lv => (lv <= 1 ? 0 : Math.floor(12 * Math.pow(lv - 1, 1.8)));
const partyChars = () => G.partyIds.map(id => G.members[id]);

function newChar(id, lv = 1) {
  const c = { id, name: CHARS[id].name, lv, exp: expFor(lv), weapon: null, armor: null };
  c.hp = stat(c, 'maxhp');
  c.mp = stat(c, 'maxmp');
  c.ap = maxAp(c);
  return c;
}

function giveExp(c, amount) {
  const msgs = [];
  c.exp += amount;
  while (c.exp >= expFor(c.lv + 1)) {
    const hp = stat(c, 'maxhp'), mp = stat(c, 'maxmp'), ap = maxAp(c), old = spellsOf(c);
    c.lv++;
    c.hp += stat(c, 'maxhp') - hp;
    c.mp += stat(c, 'maxmp') - mp;
    c.ap += maxAp(c) - ap;
    msgs.push(`${c.name}は レベル${c.lv}に あがった！`);
    spellsOf(c).filter(s => !old.includes(s)).forEach(s => msgs.push(`${c.name}は ${SPELLS[s].name}を おぼえた！`));
  }
  return msgs;
}

function restoreAll() {
  for (const c of Object.values(G.members)) {
    c.hp = stat(c, 'maxhp');
    c.mp = stat(c, 'maxmp');
    c.ap = maxAp(c);
  }
}

function addItem(id, n = 1) { G.items[id] = Math.min(99, (G.items[id] || 0) + n); }
function removeItem(id) {
  if (!G.items[id]) return;
  if (--G.items[id] <= 0) delete G.items[id];
}

// アイテムの効果。効果がなければ null
function useItemOn(id, c) {
  const it = ITEMS[id];
  const fill = (key, max, power) => {
    const before = c[key];
    c[key] = Math.min(max, c[key] + power);
    return c[key] - before;
  };
  switch (it.type) {
    case 'heal': {
      if (c.hp <= 0 || c.hp >= stat(c, 'maxhp')) return null;
      const n = fill('hp', stat(c, 'maxhp'), it.power);
      return { amount: n, msg: `${c.name}の HPが ${n} 回復した！` };
    }
    case 'mp': {
      if (c.hp <= 0 || c.mp >= stat(c, 'maxmp')) return null;
      const n = fill('mp', stat(c, 'maxmp'), it.power);
      return { amount: n, msg: `${c.name}の MPが ${n} 回復した！` };
    }
    case 'ap': {
      if (!CHARS[c.id].dragon || c.hp <= 0 || c.ap >= maxAp(c)) return null;
      const n = fill('ap', maxAp(c), it.power);
      return { amount: n, msg: `${c.name}の APが ${n} 回復した！` };
    }
    case 'revive':
      if (c.hp > 0) return null;
      c.hp = Math.floor(stat(c, 'maxhp') / 2);
      return { amount: c.hp, msg: `${c.name}は 意識を 取りもどした！` };
  }
  return null;
}

function joinParty(id) {
  const c = G.members[id], ren = G.members.ren;
  if (c.lv < ren.lv) { c.lv = ren.lv; c.exp = expFor(c.lv); }
  c.hp = stat(c, 'maxhp');
  c.mp = stat(c, 'maxmp');
  G.partyIds.push(id);
  G.flags[id] = true;
  Sound.play('join');
}

// ---------- セーブ ----------
function saveGame() {
  try { localStorage.setItem(SAVE_KEY, JSON.stringify(G)); return true; } catch (e) { return false; }
}
function loadGame() {
  try {
    const s = localStorage.getItem(SAVE_KEY);
    if (!s) return false;
    G = JSON.parse(s);
    return true;
  } catch (e) { return false; }
}
function hasSave() {
  try { return !!localStorage.getItem(SAVE_KEY); } catch (e) { return false; }
}

function newGame() {
  G = {
    members: { ren: newChar('ren'), mina: newChar('mina'), garo: newChar('garo', 2) },
    partyIds: ['ren'],
    gold: 60,
    items: { herb: 3 },
    genes: [],
    flags: {},
    opened: {},
    broken: {},
    map: 'village', x: 4, y: 5, dir: 'up',
  };
}

// ---------- イベント ----------
const INTRO = [
  'ここは 山あいの小さな村、ルフ。',
  'いにしえの竜の血をひく青年 レンは、村の長老に呼ばれていた。',
  '（矢印キーで移動 / Zで調べる・決定 / Xでメニュー・キャンセル）',
];

function doorElder() {
  if (!G.flags.quest) {
    say([
      '長老|おお、レン。よく来てくれた。',
      '長老|東の山にある『竜骨の洞窟』に、近ごろ恐ろしい魔物が棲みついたのじゃ。',
      '長老|おまえの体には、いにしえの竜の血が流れておる。',
      '長老|洞窟に眠るという『竜の遺伝子』を見つければ、その力を目覚めさせられるはずじゃ。',
      '長老|どうか魔物を退治してきてくれ。……ミーナにも声をかけてみるとよい。',
    ], () => { G.flags.quest = true; });
  } else if (G.flags.boss && !G.flags.ending) {
    say([
      '長老|なんと……竜骨のゴーレムを倒したというのか！',
      '長老|その力、まさしく竜の血の目覚めじゃ。',
      '長老|だが レン、竜の力は大きい。使い方を誤れば、身を滅ぼすこともある。',
      '長老|仲間を大切にな。それが力を正しく使う道じゃ。',
      '～ 第一章「目覚め」 完 ～',
      '（このまま冒険を続けられます。遊んでくれてありがとう！）',
    ], () => { G.flags.ending = true; restoreAll(); });
  } else if (G.flags.ending) {
    say('長老|レン、おまえの旅は まだ始まったばかりじゃ。');
  } else if (!G.flags.mina) {
    say('長老|ミーナにも声をかけてみるとよい。あの子の魔法は きっと役に立つ。');
  } else {
    say('長老|竜骨の洞窟は 村を出て北東じゃ。川の橋を渡って行くがよい。');
  }
}

function doorInn() {
  say(['宿屋|いらっしゃい！ 一晩 10Gだよ。泊まっていくかい？'], i => {
    if (i !== 0) { say('宿屋|またどうぞ。'); return; }
    if (G.gold < 10) { say('宿屋|おや、お金が足りないみたいだね。'); return; }
    G.gold -= 10;
    restoreAll();
    const ok = saveGame();
    Game.push(new Fade(() => say(['宿屋|ゆっくりお休み。', `体力が すっかり回復した！${ok ? '（セーブしました）' : ''}`]), 40));
  }, ['泊まる', 'やめる']);
}

function doorShop() {
  say('道具屋|いらっしゃい！ 旅の支度なら うちにおまかせ。', () => Game.push(new Shop()));
}

const DOORS = {
  village: { '4,4': doorElder, '13,4': doorInn, '4,10': doorShop },
};

function talkMina() {
  if (!G.flags.quest) { say('ミーナ|レン、長老さまが呼んでたよ。早く行ってあげて！'); return; }
  say([
    'ミーナ|洞窟に行くの？ ……やっぱり。',
    'ミーナ|わたしも行く！ 回復と攻撃の魔法なら まかせて。',
    'ミーナが 仲間に加わった！',
  ], () => joinParty('mina'));
}

function talkGaro() {
  if (!G.flags.quest) { say('ガロ|なんだ、ガキ。用がないなら あっち行きな。'); return; }
  say([
    'ガロ|おう、おまえら 竜骨の洞窟に行くのか？',
    'ガロ|奥は でかい岩で ふさがってるぜ。俺の腕っぷしなら 砕けるがな。',
    'ガロ|……退屈してたところだ。連れてけよ。',
    'ガロが 仲間に加わった！',
    '（ガロがいれば、大きな岩の前でZを押すと 砕いて進める）',
  ], () => joinParty('garo'));
}

function talkBoss() {
  say([
    '？？？|グオオオ……竜の血の においがする……',
    '？？？|竜の血を……喰らわせろォ……！',
  ], () => startBattle(['golem'], {
    boss: true,
    onWin: () => {
      G.flags.boss = true;
      say(['竜骨のゴーレムは 音をたてて 崩れ落ちた……', '洞窟に 静けさが もどった。村に帰って 長老に報告しよう。']);
    },
  }));
}

const CHEST_ITEMS = {
  c_fire:   { gene: 'fire' },
  c_herb:   { item: 'herb', n: 3 },
  c_thunder:{ gene: 'thunder' },
  c_gold:   { gold: 120 },
  c_revive: { item: 'revive', n: 1 },
  c_tear:   { item: 'tear', n: 2 },
  c_potion: { item: 'potion', n: 2 },
};

function openChest(e) {
  if (G.opened[e.id]) { say('宝箱は からっぽだ。'); return; }
  G.opened[e.id] = true;
  Sound.play('chest');
  const c = CHEST_ITEMS[e.id];
  if (c.gene) {
    G.genes.push(c.gene);
    const lines = [`宝箱を あけた！ ${GENES[c.gene].name}を 手に入れた！`];
    if (G.genes.length === 1) {
      lines.push('レンの中で 何かが 目覚めた……！', '戦闘で『竜変身』が 使えるようになった！ （変身中は 毎ターン APを消費する）');
    } else {
      lines.push('ふたつの遺伝子が 共鳴している……！', '新たな変身『ツインドラゴン』が 使えるようになった！');
    }
    say(lines);
  } else if (c.gold) {
    G.gold += c.gold;
    say(`宝箱を あけた！ ${c.gold}Gを 手に入れた！`);
  } else {
    addItem(c.item, c.n);
    say(`宝箱を あけた！ ${ITEMS[c.item].name}を ${c.n}こ 手に入れた！`);
  }
}

const ENTITIES = {
  village: [
    { id: 'mina', x: 7, y: 6, sprite: 'mina', cond: () => !G.flags.mina, talk: talkMina },
    { id: 'v1', x: 15, y: 12, sprite: 'villager1',
      talk: () => say('村人|東の洞窟には、昔 竜が眠っていたって 言い伝えがあるんだ。') },
    { id: 'v2', x: 6, y: 12, sprite: 'villager2',
      talk: () => say(['村人|池の そばの看板の前で Zを押すと、釣りができるぞ。', '村人|大物は 腹の足しにもなるし、たまに 金になるのも かかるって話だ。']) },
    { id: 'kid', x: 12, y: 6, sprite: 'child',
      talk: () => say(G.genes.length ? 'こども|すげー！ レンにいちゃん、ほんとに竜になれるんだ！' : 'こども|レンにいちゃん、竜に変身できるって ほんと？') },
  ],
  world: [
    { id: 'garo', x: 23, y: 2, sprite: 'garo', cond: () => !G.flags.garo, talk: talkGaro },
  ],
  cave: [
    { id: 'c_fire', x: 1, y: 7, chest: true },
    { id: 'c_herb', x: 1, y: 11, chest: true },
    { id: 'c_thunder', x: 21, y: 7, chest: true },
    { id: 'c_gold', x: 21, y: 13, chest: true },
    { id: 'c_revive', x: 21, y: 15, chest: true },
    { id: 'c_potion', x: 4, y: 14, chest: true },
    { id: 'c_tear', x: 5, y: 1, chest: true },
    { id: 'boss', x: 11, y: 2, sprite: 'golem', big: true, cond: () => !G.flags.boss, talk: talkBoss },
  ],
};

function entitiesOf(map) {
  return (ENTITIES[map] || []).filter(e => !e.cond || e.cond());
}

function startBattle(ids, opts = {}) {
  Sound.play('encounter');
  Game.push(new Fade(() => Game.push(new Battle(ids, opts)), 14));
}

// ---------- フィールド ----------
const DIRS = { up: [0, -1], down: [0, 1], left: [-1, 0], right: [1, 0] };

class Field {
  constructor() { this.reset(); }
  reset() {
    this.moving = false;
    this.t = 0;
    this.label = 150;
    this.enc = rand(14, 28);
    const n = G.partyIds.length - 1;
    this.trail = Array.from({ length: n }, () => ({ x: G.x, y: G.y }));
    this.trailFrom = this.trail.map(p => ({ ...p }));
  }
  // 途中で仲間が増えたら隊列を伸ばす
  syncTrail() {
    while (this.trail.length < G.partyIds.length - 1) {
      const last = this.trail[this.trail.length - 1] || { x: G.x, y: G.y };
      this.trail.push({ ...last });
      this.trailFrom.push({ ...last });
    }
  }
  get map() { return MAPS[G.map]; }
  tileAt(x, y) {
    const row = this.map.rows[y];
    if (!row || x < 0 || x >= row.length) return ' ';
    const ch = row[x];
    if (ch === 'R' && G.broken[`${G.map}:${x},${y}`]) return 'f';
    return ch;
  }
  entityAt(x, y) { return entitiesOf(G.map).find(e => e.x === x && e.y === y); }
  blocked(x, y) { return SOLID.has(this.tileAt(x, y)) || !!this.entityAt(x, y); }

  warp(map, x, y, dir) {
    Game.push(new Fade(() => {
      G.map = map; G.x = x; G.y = y; G.dir = dir;
      this.reset();
    }));
  }

  update() {
    this.syncTrail();
    if (this.label > 0) this.label--;
    if (this.moving) {
      this.t += 1 / 8;
      if (this.t >= 1) {
        this.moving = false;
        G.x = this.tx; G.y = this.ty;
        this.onStep();
      }
      return;
    }
    if (Input.pressed.cancel) { Game.push(new FieldMenu()); return; }
    if (Input.pressed.ok) { this.interact(); return; }
    for (const d of ['up', 'down', 'left', 'right']) {
      if (!Input.held[d] && !Input.pressed[d]) continue;
      G.dir = d;
      const [dx, dy] = DIRS[d];
      const nx = G.x + dx, ny = G.y + dy;
      if (!this.blocked(nx, ny)) {
        // 仲間はリーダーの通った跡をついてくる
        this.trailFrom = this.trail.map(p => ({ ...p }));
        this.trail = [{ x: G.x, y: G.y }, ...this.trail].slice(0, this.trail.length);
        this.moving = true;
        this.t = 0;
        this.tx = nx; this.ty = ny;
      } else if (Input.pressed[d] && this.tileAt(nx, ny) === 'D') {
        this.interact();
      }
      break;
    }
  }

  onStep() {
    const ch = this.tileAt(G.x, G.y);
    if (G.map === 'village' && ch === 'E') return this.warp('world', 5, 18, 'down');
    if (G.map === 'world' && ch === 'v') return this.warp('village', 9, 13, 'up');
    if (G.map === 'world' && ch === 'c') return this.warp('cave', 11, 17, 'up');
    if (G.map === 'cave' && ch === '<') return this.warp('world', 24, 3, 'down');
    const enc = this.map.encounters;
    if (enc) {
      this.enc -= ch === '*' ? 2 : 1;
      if (this.enc <= 0) {
        this.enc = rand(14, 28);
        startBattle(pick(ch === '*' && enc.forest ? enc.forest : enc.normal));
      }
    }
  }

  interact() {
    const [dx, dy] = DIRS[G.dir];
    const x = G.x + dx, y = G.y + dy;
    const e = this.entityAt(x, y);
    if (e) { e.chest ? openChest(e) : e.talk(e); return; }
    const ch = this.tileAt(x, y);
    if (ch === 'D') {
      const fn = (DOORS[G.map] || {})[`${x},${y}`];
      if (fn) fn(); else say('カギが かかっている。');
    } else if (ch === 'R') {
      if (G.partyIds.includes('garo')) {
        say(['ガロ|どいてな。……どりゃあっ！'], () => {
          G.broken[`${G.map}:${x},${y}`] = true;
          Sound.play('smash');
          say('ガロが 大岩を 粉々に砕いた！');
        });
      } else {
        say('大きな岩が 道をふさいでいる。びくともしない……');
      }
    } else if (ch === 'F') {
      say(['釣り場の看板だ。「よく釣れます」', '釣りを していく？'], i => { if (i === 0) Game.push(new Fishing()); }, ['する', 'しない']);
    }
  }

  // ---------- 描画 ----------
  lerpPos(from, to) {
    const t = this.moving ? this.t : 1;
    return { x: (from.x + (to.x - from.x) * t) * TILE, y: (from.y + (to.y - from.y) * t) * TILE };
  }
  draw() {
    const map = this.map;
    const mw = map.rows[0].length * TILE, mh = map.rows.length * TILE;
    const p = this.moving ? this.lerpPos({ x: G.x, y: G.y }, { x: this.tx, y: this.ty }) : { x: G.x * TILE, y: G.y * TILE };
    const cx = Math.round(mw <= W ? (mw - W) / 2 : clamp(p.x + 16 - W / 2, 0, mw - W));
    const cy = Math.round(mh <= H ? (mh - H) / 2 : clamp(p.y + 16 - H / 2, 0, mh - H));
    const f = (Game.frame >> 5) % 4;
    const x0 = Math.floor(cx / TILE), y0 = Math.floor(cy / TILE);
    for (let ty = y0; ty <= y0 + H / TILE + 1; ty++) {
      for (let tx = x0; tx <= x0 + W / TILE + 1; tx++) {
        ctx.drawImage(getTile(this.tileAt(tx, ty), f), tx * TILE - cx, ty * TILE - cy);
      }
    }
    // 人物 (y順)
    const draws = [];
    for (const e of entitiesOf(G.map)) {
      draws.push({ y: e.y * TILE, fn: () => {
        const sx = e.x * TILE - cx, sy = e.y * TILE - cy;
        if (e.chest) this.drawChest(sx, sy, G.opened[e.id]);
        else if (e.big) drawSprite(e.sprite, sx - 16, sy - 32 + Math.sin(Game.frame / 20) * 2, 4);
        else drawSprite(e.sprite, sx, sy, 2);
      } });
    }
    const bob = this.moving && Math.floor(this.t * 4) % 2 ? -2 : 0;
    const back = G.dir === 'up' ? '_back' : '';
    this.syncTrail();
    G.partyIds.slice(1).forEach((id, i) => {
      const fp = this.moving ? this.lerpPos(this.trailFrom[i], this.trail[i]) : { x: this.trail[i].x * TILE, y: this.trail[i].y * TILE };
      if (!this.moving && this.trail[i].x === G.x && this.trail[i].y === G.y) return;
      draws.push({ y: fp.y - 0.5 - i * 0.01, fn: () => drawSprite(CHARS[id].sprite + back, fp.x - cx, fp.y - cy + bob, 2) });
    });
    draws.push({ y: p.y, fn: () => drawSprite('ren' + back, p.x - cx, p.y - cy + bob, 2, { flip: G.dir === 'left' }) });
    draws.sort((a, b) => a.y - b.y).forEach(d => d.fn());

    if (map.dark) {
      const px = p.x - cx + 16, py = p.y - cy + 16;
      const g = ctx.createRadialGradient(px, py, 70, px, py, 280);
      g.addColorStop(0, 'rgba(0,0,0,0)');
      g.addColorStop(1, 'rgba(0,0,0,0.88)');
      ctx.fillStyle = g;
      ctx.fillRect(0, 0, W, H);
    }
    if (this.label > 0) {
      ctx.globalAlpha = Math.min(1, this.label / 30);
      drawWindow(16, 16, 200, 44);
      drawText(map.name, 116, 27, '#fff', 20, 'center');
      ctx.globalAlpha = 1;
    }
  }
  drawChest(x, y, open) {
    ctx.fillStyle = '#1b1424';
    ctx.fillRect(x + 4, y + 10, 24, 18);
    ctx.fillStyle = open ? '#5a3a1c' : '#a8642a';
    ctx.fillRect(x + 6, y + 12, 20, 14);
    ctx.fillStyle = '#f2d04b';
    ctx.fillRect(x + 6, y + 17, 20, 2);
    ctx.fillRect(x + 14, y + 15, 4, 6);
    if (open) { ctx.fillStyle = '#1b1424'; ctx.fillRect(x + 6, y + 12, 20, 4); }
  }
}

// ---------- フィールドメニュー ----------
class FieldMenu {
  constructor() {
    this.mode = 'main';
    this.main = new Menu(['ステータス', 'まほう', 'アイテム', 'セーブ', 'とじる'].map(label => ({ label })));
    this.note = '';
    this.noteT = 0;
  }
  memberMenu(okFn) {
    return new Menu(partyChars().map(c => ({ label: c.name, c, disabled: okFn ? !okFn(c) : false })));
  }
  flash(msg) { this.note = msg; this.noteT = 120; }
  spellMenu() {
    return new Menu(spellsOf(this.sel).map(s => ({
      label: SPELLS[s].name, right: `MP${SPELLS[s].mp}`, v: s,
      disabled: !SPELLS[s].field || this.sel.mp < SPELLS[s].mp,
    })), { rows: 6 });
  }
  itemMenu() {
    return new Menu(Object.keys(G.items).map(id => ({ label: ITEMS[id].name, right: `×${G.items[id]}`, v: id })), { rows: 6 });
  }
  castField(target) {
    const s = SPELLS[this.spell], caster = this.sel;
    const ts = (target ? [target] : partyChars()).filter(c => c.hp > 0 && c.hp < stat(c, 'maxhp'));
    if (!ts.length) { this.flash('効果がない。'); return; }
    caster.mp -= s.mp;
    ts.forEach(c => { c.hp = Math.min(stat(c, 'maxhp'), c.hp + healAmount(stat(caster, 'mag'), s)); });
    Sound.play('heal');
    this.flash(`${caster.name}の ${s.name}！ HPが回復した。`);
    this.sm = this.spellMenu();
    this.mode = 'spell';
  }
  update() {
    if (this.noteT > 0) this.noteT--;
    switch (this.mode) {
      case 'main': {
        const r = this.main.update();
        if (r === 'cancel') { Game.pop(); return; }
        if (r !== 'ok') return;
        const l = this.main.cur.label;
        if (l === 'ステータス') { this.purpose = 'status'; this.mm = this.memberMenu(); this.mode = 'member'; }
        else if (l === 'まほう') {
          this.purpose = 'magic';
          this.mm = this.memberMenu(c => spellsOf(c).some(s => SPELLS[s].field));
          if (this.mm.items.every(it => it.disabled)) this.flash('使える魔法がない。');
          else { this.mm.i = this.mm.items.findIndex(it => !it.disabled); this.mode = 'member'; }
        } else if (l === 'アイテム') {
          this.im = this.itemMenu();
          if (!this.im.items.length) this.flash('アイテムを 持っていない。');
          else this.mode = 'items';
        } else if (l === 'セーブ') this.flash(saveGame() ? 'セーブしました。' : 'セーブできませんでした。');
        else Game.pop();
        break;
      }
      case 'member': {
        const r = this.mm.update();
        if (r === 'cancel') { this.mode = 'main'; return; }
        if (r !== 'ok') return;
        this.sel = this.mm.cur.c;
        if (this.purpose === 'status') this.mode = 'status';
        else { this.sm = this.spellMenu(); this.mode = 'spell'; }
        break;
      }
      case 'status':
        if (Input.pressed.cancel || Input.pressed.ok) { Sound.play('cancel'); this.mode = 'member'; }
        break;
      case 'spell': {
        const r = this.sm.update();
        if (r === 'cancel') { this.mode = 'member'; return; }
        if (r !== 'ok') return;
        this.spell = this.sm.cur.v;
        if (SPELLS[this.spell].target === 'allies') this.castField(null);
        else { this.tm = this.memberMenu(); this.mode = 'spellTarget'; }
        break;
      }
      case 'spellTarget': {
        const r = this.tm.update();
        if (r === 'cancel') { this.mode = 'spell'; return; }
        if (r === 'ok') this.castField(this.tm.cur.c);
        break;
      }
      case 'items': {
        const r = this.im.update();
        if (r === 'cancel') { this.mode = 'main'; return; }
        if (r !== 'ok') return;
        this.item = this.im.cur.v;
        this.tm = this.memberMenu();
        this.mode = 'itemTarget';
        break;
      }
      case 'itemTarget': {
        const r = this.tm.update();
        if (r === 'cancel') { this.mode = 'items'; return; }
        if (r !== 'ok') return;
        const res = useItemOn(this.item, this.tm.cur.c);
        if (!res) { this.flash('効果がない。'); return; }
        removeItem(this.item);
        Sound.play('heal');
        this.flash(res.msg);
        const i = this.im.i;
        this.im = this.itemMenu();
        this.im.i = Math.min(i, Math.max(0, this.im.items.length - 1));
        if (!G.items[this.item]) this.mode = this.im.items.length ? 'items' : 'main';
        break;
      }
    }
  }
  draw() {
    drawWindow(12, 12, 420, 456);
    const picking = { member: this.mm, spellTarget: this.tm, itemTarget: this.tm }[this.mode];
    partyChars().forEach((c, i) => {
      const y = 28 + i * 140;
      if (picking && picking.i === i) {
        ctx.fillStyle = 'rgba(255,255,255,0.12)';
        ctx.fillRect(22, y - 6, 400, 128);
        drawText('▶', 24, y + 40, '#ffe080', 20);
      }
      drawSprite(CHARS[c.id].sprite, 46, y + 16, 4);
      drawText(c.name, 130, y, '#fff', 22);
      drawText(`Lv ${c.lv}`, 410, y, '#ffe080', 20, 'right');
      const row = (label, cur, max, col, yy) => {
        drawText(label, 130, yy, '#aac', 18);
        drawText(`${cur} / ${max}`, 410, yy, cur <= 0 && label === 'HP' ? '#f66' : '#fff', 18, 'right');
        drawBar(170, yy + 22, 240, 5, cur / max, col);
      };
      row('HP', c.hp, stat(c, 'maxhp'), '#5c5', y + 30);
      row('MP', c.mp, stat(c, 'maxmp'), '#58f', y + 60);
      if (CHARS[c.id].dragon) row('AP', c.ap, maxAp(c), '#f94', y + 90);
    });
    drawWindow(444, 12, 184, 190);
    this.main.draw(456, 28, 150, { active: this.mode === 'main' });
    drawWindow(444, 412, 184, 56);
    drawText(`${G.gold} G`, 610, 428, '#ffe080', 22, 'right');
    drawWindow(444, 214, 184, 56);
    drawText(MAPS[G.map].name, 536, 230, '#fff', 18, 'center');

    if (this.mode === 'spell') {
      drawWindow(120, 60, 400, 260);
      drawText(`${this.sel.name}の まほう`, 140, 74, '#ffe080', 18);
      this.sm.draw(140, 104, 360);
    }
    if (this.mode === 'items') {
      drawWindow(120, 40, 400, 330);
      this.im.draw(140, 60, 360, { lineH: 34 });
      if (this.im.cur) {
        wrapText(ITEMS[this.im.cur.v].desc, 360, 18).forEach((l, i) => drawText(l, 140, 284 + i * 24, '#cce', 18));
      }
    }
    if (this.mode === 'status') this.drawStatus(this.sel);
    if (this.noteT > 0) {
      drawWindow(40, 404, 392, 56);
      drawText(this.note, 60, 421, '#fff', 18);
    }
  }
  drawStatus(c) {
    drawWindow(30, 24, 580, 432);
    drawSprite(CHARS[c.id].sprite, 56, 56, 6);
    drawText(c.name, 170, 50, '#fff', 28);
    drawText(`レベル ${c.lv}`, 170, 90, '#ffe080', 20);
    drawText(`経験値 ${c.exp}`, 170, 118, '#fff', 18);
    drawText(`次のレベルまで ${expFor(c.lv + 1) - c.exp}`, 170, 142, '#fff', 18);
    Object.keys(STAT_NAMES).forEach((k, i) => {
      drawText(STAT_NAMES[k], 60, 190 + i * 28, '#aac', 18);
      drawText(String(stat(c, k)), 230, 190 + i * 28, '#fff', 18, 'right');
    });
    drawText('武器', 280, 190, '#aac', 18);
    drawText(c.weapon ? EQUIPS[c.weapon].name : 'なし', 340, 190, '#fff', 18);
    drawText('防具', 280, 218, '#aac', 18);
    drawText(c.armor ? EQUIPS[c.armor].name : 'なし', 340, 218, '#fff', 18);
    drawText(c.id === 'garo' ? 'わざ' : 'まほう', 280, 256, '#aac', 18);
    drawText(spellsOf(c).map(s => SPELLS[s].name).join('  ') || 'なし', 340, 256, '#fff', 18);
    if (CHARS[c.id].dragon) {
      drawText('遺伝子', 280, 290, '#aac', 18);
      drawText(G.genes.map(g => GENES[g].name).join('  ') || 'なし', 360, 290, '#fc8', 18);
      drawText('変身', 280, 318, '#aac', 18);
      const forms = availableForms().map(f => `${FORMS[f].name}(AP${FORMS[f].cost})`);
      (forms.length ? forms : ['なし']).forEach((t, i) => drawText(t, 340, 318 + i * 26, '#fc8', 18));
    }
    drawText('Z / X で もどる', 590, 420, '#889', 16, 'right');
  }
}

// ---------- 道具屋 ----------
class Shop {
  constructor() {
    this.menu = new Menu(this.entries(), { rows: 8 });
    this.note = '何を 買うかい？';
  }
  entries() {
    return SHOP_LIST.map(id => {
      if (ITEMS[id]) return { id, label: ITEMS[id].name, right: `${ITEMS[id].price}G` };
      const e = EQUIPS[id];
      const owned = G.members[e.who][e.slot] === id;
      return { id, label: e.name, right: owned ? 'そうび中' : `${e.price}G`, disabled: owned };
    });
  }
  update() {
    const r = this.menu.update();
    if (r === 'cancel') { Game.pop(); say('道具屋|まいどあり！ また来ておくれ。'); return; }
    if (r !== 'ok') return;
    const id = this.menu.cur.id;
    const it = ITEMS[id] || EQUIPS[id];
    if (G.gold < it.price) { Sound.play('buzz'); this.note = 'お金が 足りないよ。'; return; }
    if (ITEMS[id]) {
      if ((G.items[id] || 0) >= 99) { this.note = 'それ以上 持てないよ。'; return; }
      G.gold -= it.price;
      addItem(id);
      this.note = `${it.name}を 買った！（所持 ${G.items[id]}）`;
    } else {
      G.gold -= it.price;
      G.members[it.who][it.slot] = id;
      this.note = `${it.name}を ${CHARS[it.who].name}に そうびした！`;
      const i = this.menu.i;
      this.menu = new Menu(this.entries(), { rows: 8 });
      this.menu.i = i;
    }
    Sound.play('chest');
  }
  draw() {
    drawWindow(20, 20, 400, 320);
    drawText('道具屋', 40, 32, '#ffe080', 20);
    this.menu.draw(40, 66, 350);
    drawWindow(432, 20, 188, 60);
    drawText(`${G.gold} G`, 600, 38, '#ffe080', 22, 'right');
    drawWindow(20, 350, 600, 110);
    const cur = this.menu.cur;
    let desc;
    if (ITEMS[cur.id]) desc = `${ITEMS[cur.id].desc}　（所持 ${G.items[cur.id] || 0}）`;
    else {
      const e = EQUIPS[cur.id];
      const now = G.members[e.who][e.slot];
      const bonus = Object.entries(e.bonus).map(([k, v]) => `${STAT_NAMES[k]}+${v}`).join(' ');
      desc = `${CHARS[e.who].name}用の${e.slot === 'weapon' ? '武器' : '防具'}。${bonus}　（いま: ${now ? EQUIPS[now].name : 'なし'}）`;
    }
    wrapText(desc, 560, 18).forEach((l, i) => drawText(l, 40, 364 + i * 24, '#fff', 18));
    drawText(this.note, 40, 424, '#cce', 18);
    if (EQUIPS[cur.id]) drawSprite(CHARS[EQUIPS[cur.id].who].sprite, 480, 120, 6);
  }
}

// ---------- 釣り ----------
const FISH = [
  { id: 'boot', name: '古い長靴', w: 20, speed: 0.012, zone: 0.3 },
  { id: 'trout', name: 'ニジマス', w: 50, speed: 0.02, zone: 0.22 },
  { id: 'bigfish', name: '大ナマズ', w: 20, speed: 0.03, zone: 0.16 },
  { id: 'goldcarp', name: '黄金のコイ', w: 10, speed: 0.042, zone: 0.12 },
];
function rollFish() {
  let r = Math.random() * FISH.reduce((s, f) => s + f.w, 0);
  for (const f of FISH) { if ((r -= f.w) < 0) return f; }
  return FISH[0];
}

class Fishing {
  constructor() { this.toReady(); }
  toReady() { this.state = 'ready'; this.t = 0; this.msg = 'Zで 竿を投げる　/　Xで やめる'; }
  catchFish() {
    const f = this.fish;
    Sound.play('victory');
    this.state = 'result';
    if (f.id === 'goldcarp') { G.gold += 100; this.msg = '黄金のコイを 釣りあげた！ 売って 100G を手に入れた！'; }
    else if (f.id === 'boot') this.msg = '古い長靴を 釣りあげた……。そっと池に もどした。';
    else { addItem(f.id); this.msg = `${f.name}を 釣りあげた！`; }
  }
  update() {
    this.t++;
    const ok = Input.pressed.ok;
    switch (this.state) {
      case 'ready':
        if (ok) { Sound.play('ok'); this.state = 'wait'; this.wait = rand(60, 220); this.t = 0; this.msg = '……　（アタリを待て）'; }
        else if (Input.pressed.cancel) { Sound.play('cancel'); Game.pop(); }
        break;
      case 'wait':
        if (ok) { Sound.play('buzz'); this.state = 'result'; this.msg = 'あわてすぎた！ 魚が 逃げてしまった。'; break; }
        if (this.t >= this.wait) { Sound.play('bite'); this.state = 'bite'; this.t = 0; this.msg = '！ アタリだ！ Zを押せ！'; }
        break;
      case 'bite':
        if (ok) {
          this.fish = rollFish();
          Object.assign(this, { state: 'reel', t: 0, pos: 0, vel: this.fish.speed, hits: 0, miss: 0 });
          this.zone = Math.random() * (1 - this.fish.zone);
          this.msg = 'カーソルが 緑のゾーンに来たら Z！（3回成功で 釣りあげ）';
        } else if (this.t > 40) { this.state = 'result'; this.msg = '逃げられてしまった……'; }
        break;
      case 'reel': {
        this.pos += this.vel;
        if (this.pos > 1) { this.pos = 1; this.vel = -this.vel; }
        if (this.pos < 0) { this.pos = 0; this.vel = -this.vel; }
        if (!ok) break;
        const f = this.fish;
        if (this.pos >= this.zone && this.pos <= this.zone + f.zone) {
          this.hits++;
          Sound.play('hit');
          this.zone = Math.random() * (1 - f.zone);
          if (this.hits >= 3) this.catchFish();
        } else {
          this.miss++;
          Sound.play('buzz');
          if (this.miss >= 2) { this.state = 'result'; this.msg = '糸が 切れてしまった……'; }
        }
        break;
      }
      case 'result':
        if (ok || Input.pressed.cancel) this.toReady();
        break;
    }
  }
  draw() {
    const g = ctx.createLinearGradient(0, 0, 0, 140);
    g.addColorStop(0, '#f0a868'); g.addColorStop(1, '#f8d8a0');
    ctx.fillStyle = g; ctx.fillRect(0, 0, W, 140);
    ctx.fillStyle = '#3e7a3a'; ctx.fillRect(0, 120, W, 30);
    ctx.fillStyle = '#2a64b8'; ctx.fillRect(0, 150, W, 330);
    ctx.fillStyle = '#5a94e0';
    for (let i = 0; i < 14; i++) {
      const x = ((i * 97 + Game.frame * (0.3 + (i % 3) * 0.2)) % (W + 60)) - 30;
      ctx.fillRect(x, 170 + i * 21, 30 + (i % 4) * 10, 2);
    }
    ctx.fillStyle = '#7a5a3a'; ctx.fillRect(0, 380, 200, 100);
    drawSprite('ren_back', 70, 330, 4);
    // 竿と浮き
    const casting = this.state !== 'ready';
    const bx = 400, by = 260 + (this.state === 'wait' ? Math.sin(this.t / 10) * 3 : 0) + (this.state === 'bite' ? 10 : 0);
    ctx.strokeStyle = '#5a3a1a'; ctx.lineWidth = 4;
    ctx.beginPath(); ctx.moveTo(130, 350); ctx.lineTo(220, 230); ctx.stroke();
    if (casting) {
      ctx.strokeStyle = '#eee'; ctx.lineWidth = 1;
      ctx.beginPath(); ctx.moveTo(220, 230); ctx.quadraticCurveTo(320, 220, bx, by); ctx.stroke();
      ctx.fillStyle = '#e33'; ctx.beginPath(); ctx.arc(bx, by, 6, 0, Math.PI * 2); ctx.fill();
      ctx.fillStyle = '#fff'; ctx.fillRect(bx - 6, by - 1, 12, 2);
      if (this.state === 'bite') drawText('！', bx + 10, by - 50, '#ff4', 40);
    }
    if (this.state === 'reel') {
      drawWindow(220, 380, 400, 88);
      const x0 = 244, w = 352;
      ctx.fillStyle = '#0a0a20'; ctx.fillRect(x0, 402, w, 24);
      ctx.fillStyle = '#4c4'; ctx.fillRect(x0 + this.zone * w, 404, this.fish.zone * w, 20);
      ctx.fillStyle = '#fff'; ctx.fillRect(x0 + this.pos * w - 2, 394, 4, 40);
      drawText('●'.repeat(this.hits) + '○'.repeat(3 - this.hits), 420, 438, '#ffe080', 16, 'center');
    }
    drawWindow(16, 16, 608, 60);
    drawText(this.msg, 36, 34, '#fff', 19);
  }
}

// ---------- タイトル / ゲームオーバー ----------
class Title {
  constructor() {
    this.menu = new Menu([{ label: 'はじめから' }, { label: 'つづきから', disabled: !hasSave() }]);
    if (hasSave()) this.menu.i = 1;
  }
  update() {
    if (this.menu.update() !== 'ok') return;
    const fresh = this.menu.i === 0;
    if (fresh) newGame(); else loadGame();
    Game.push(new Fade(() => {
      Game.replace(new Field());
      if (fresh) say(INTRO);
    }, 24));
  }
  draw() {
    const g = ctx.createLinearGradient(0, 0, 0, H);
    g.addColorStop(0, '#060818'); g.addColorStop(0.6, '#24164a'); g.addColorStop(1, '#6a2a3a');
    ctx.fillStyle = g; ctx.fillRect(0, 0, W, H);
    for (let i = 0; i < 60; i++) {
      const tw = (Math.sin(Game.frame / 30 + i) + 1) / 2;
      ctx.fillStyle = `rgba(255,255,255,${0.3 + tw * 0.6})`;
      ctx.fillRect((i * 137) % W, (i * 71) % 260, 2, 2);
    }
    drawSprite('dragon_fire', 360, 60 + Math.sin(Game.frame / 40) * 6, 14, { alpha: 0.35, flip: true });
    drawText('竜血の旅路', W / 2, 120, '#ffe0a0', 64, 'center');
    drawText('～ Dragon Blood Saga ～', W / 2, 200, '#e0c0ff', 22, 'center');
    drawWindow(230, 290, 180, 96);
    this.menu.draw(250, 306, 140);
    drawText('矢印キー：移動　Z：決定　X：メニュー/もどる', W / 2, 430, '#aab', 16, 'center');
  }
}

class GameOver {
  constructor() { this.menu = new Menu([{ label: '最後のセーブから' }, { label: 'タイトルへ' }]); this.t = 0; }
  update() {
    this.t++;
    if (this.t < 40 || this.menu.update() !== 'ok') return;
    if (this.menu.i === 0 && loadGame()) {
      Game.push(new Fade(() => Game.replace(new Field()), 24));
    } else {
      Game.push(new Fade(() => Game.replace(new Title()), 24));
    }
  }
  draw() {
    ctx.fillStyle = '#000'; ctx.fillRect(0, 0, W, H);
    drawText('全滅してしまった……', W / 2, 150, '#d44', 36, 'center');
    if (this.t >= 40) {
      drawWindow(200, 260, 240, 96);
      this.menu.draw(220, 276, 200);
    }
  }
}

// ---------- 起動 ----------
Game.push(new Title());
startLoop();
