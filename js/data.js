'use strict';
// ゲームデータ: スプライト / キャラクター / 魔法 / アイテム / 敵 / マップ

// ---------- スプライト (16x16, 1文字 = 1ドット, '.' は透明) ----------
const BASE_PAL = {
  k: '#1b1424', s: '#f6c89f', w: '#ffffff', r: '#d83a3a', y: '#f2d04b',
  o: '#8a5a2b', e: '#6a6a7a',
};

// 人物は 16x32 ドットの 4 頭身 (頭 8 ドット + 体 24 ドット)
const HUMAN = [
  '................',
  '.....kkkkkk.....',
  '....kHHHHHHk....',
  '...kHHHHHHHHk...',
  '...kHHsHHsHHk...',
  '...kHskssksHk...',
  '...kHssssssHk...',
  '....kssssssk....',
  '.....kkkkkk.....',
  '......kssk......',
  '....kkCCCCkk....',
  '...kCCCCCCCCk...',
  '..kCkCCCCCCkCk..',
  '..kCkCCCCCCkCk..',
  '..kCkCCCCCCkCk..',
  '..kCkCCCCCCkCk..',
  '..kskCCCCCCksk..',
  '...kkYYYYYYkk...',
  '....kPPPPPPk....',
  '....kPPPPPPk....',
  '....kPPkkPPk....',
  '....kPPkkPPk....',
  '....kPPkkPPk....',
  '....kPPkkPPk....',
  '....kPPkkPPk....',
  '....kPPkkPPk....',
  '....kPPkkPPk....',
  '....kPPkkPPk....',
  '....kBBkkBBk....',
  '....kBBkkBBk....',
  '...kBBBkkBBBk...',
  '...kkkkkkkkkk...',
];

function makeHuman(pal, opts = {}) {
  const g = HUMAN.map(r => r.split(''));
  const put = (x, y, c) => { if (g[y] && g[y][x] === '.') g[y][x] = c; };
  if (opts.wings) {
    [[2, 10], [3, 10], [12, 10], [13, 10], [1, 11], [2, 11], [13, 11], [14, 11],
     [0, 12], [1, 12], [14, 12], [15, 12], [0, 13], [1, 13], [14, 13], [15, 13],
     [0, 14], [1, 14], [14, 14], [15, 14], [1, 15], [14, 15], [1, 16], [14, 16]]
      .forEach(([x, y]) => put(x, y, 'W'));
  }
  if (opts.ears) {
    put(4, 0, 'k'); put(11, 0, 'k'); put(4, 1, 'H'); put(11, 1, 'H');
  }
  if (opts.tail) {
    [[12, 19], [13, 19], [13, 18], [14, 18], [14, 17], [14, 16]].forEach(([x, y]) => put(x, y, 'H'));
  }
  return { rows: g.map(r => r.join('')), pal: Object.assign({ Y: '#6b4a2a' }, BASE_PAL, pal) };
}

// 後ろ向き: 顔を髪で塗りつぶす
function backOf(rows) {
  return rows.map((row, y) => {
    if (y < 4 || y > 7) return row;
    const a = row.split('');
    const first = a.indexOf('k'), last = a.lastIndexOf('k');
    for (let x = first + 1; x < last; x++) if (a[x] === 's' || a[x] === 'k') a[x] = 'H';
    return a.join('');
  });
}

const SPRITES = {};
function defHuman(name, pal, opts) {
  const s = makeHuman(pal, opts);
  SPRITES[name] = s;
  SPRITES[name + '_back'] = { rows: backOf(s.rows), pal: s.pal };
}
// レンは専用の 20x32 ドット。ツンツン髪・スカーフ・前開きのコート・だぶだぶのズボン・ブーツ・背中の大剣
const REN_FRONT = [
  '...........k..k.....',
  '.......k..kHkkHk....',
  '......kHkkHHHHHk.k..',
  '.....kHHHHHHHHHHkHk.',
  '....kHHHHHHHHHHHHHk.',
  '....kHHsHHHHsHHHHk..',
  '....kHsssssssssHk...',
  '....kHseksssekssk...',
  '.....ksssssssssk....',
  '....kOOOOsssOOOOk...',
  '...kOOOOOOOOOOOOOk..',
  '..kGGkOOTTTTLLkAAk..',
  '..kGGkOLTTTTLLkAAk..',
  '..kGGkLLTTTTLLkGGk..',
  '..kWWkLLRRRRLLkWWk..',
  '..kWWkLLRRRRLLkWWk..',
  '..ksskLLYyyYLLkssk..',
  '...kkkLLPPPPLLkkk...',
  '....kLLPPPPPPLLk....',
  '...kLLPPPPPPPPLLk...',
  '...kLPPPPPPPPPPLk...',
  '..kLLPPPPkkPPPPLLk..',
  '..kLPPPPPkkPPPPPLk..',
  '..kLPPPPPkkPPPPPLk..',
  '..kLkPPPPkkPPPPkLk..',
  '...kkGGGGkkGGGGkk...',
  '....kGGGk..kGGGk....',
  '....kBBBk..kBBBk....',
  '....kBBBk..kBBBk....',
  '...kBBBBk..kBBBBk...',
  '..kBBBBBk..kBBBBBk..',
  '..kkkkkkk..kkkkkkk..',
];

// 後ろ姿: 顔は髪、胴はコートの背中、背中に斜めの大剣
function renBack(rows) {
  const g = rows.map(r => r.split(''));
  for (let y = 5; y <= 8; y++) {
    const first = g[y].indexOf('k'), last = g[y].lastIndexOf('k');
    for (let x = first + 1; x < last; x++) g[y][x] = 'H';
  }
  g[9] = g[9].map(c => (c === 's' ? 'O' : c));
  for (let y = 11; y <= 23; y++) for (let x = 6; x <= 13; x++) if (g[y][x] !== 'k') g[y][x] = 'L';
  for (let i = 0; i <= 9; i++) { g[9 + i][15 - i] = 'S'; g[9 + i][14 - i] = 'S'; }
  [[16, 8], [17, 7], [15, 8], [17, 8], [16, 7]].forEach(([x, y]) => { g[y][x] = 'M'; });
  return g.map(r => r.join(''));
}

const REN_PAL = Object.assign({}, BASE_PAL, {
  H: '#2fb4d0', s: '#f6d2a8', e: '#2a5ab8', O: '#ec7448', T: '#9ad8e0', R: '#b83a3a',
  L: '#b8a4d8', P: '#5a6030', G: '#8a9aa0', B: '#6a3a24', Y: '#5a3a28', y: '#a8c8d0',
  A: '#b83a3a', W: '#c8b8a4', S: '#a83030', M: '#8a8a40',
});
// 正面: 肩越しに背中の大剣の鞘がのぞく
const REN_FRONT_SWORD = REN_FRONT.map(r => r.split(''));
[[19, 4], [18, 5], [19, 5], [17, 6], [18, 6], [17, 7], [16, 8]].forEach(([x, y]) => {
  if (REN_FRONT_SWORD[y][x] === '.') REN_FRONT_SWORD[y][x] = 'S';
});
SPRITES.ren = { rows: REN_FRONT_SWORD.map(r => r.join('')), pal: REN_PAL };
SPRITES.ren_back = { rows: renBack(REN_FRONT), pal: REN_PAL };

defHuman('mina', { H: '#f2d860', C: '#e2588c', P: '#e2588c', B: '#8a4a2a', W: '#ffffff' }, { wings: true });
defHuman('garo', { H: '#d07a2a', s: '#eaa060', C: '#4f7a3a', P: '#5a4030', B: '#3a2a1a' }, { ears: true, tail: true });
defHuman('elder', { H: '#d6d6d6', C: '#7a4aa0', P: '#7a4aa0', B: '#4a2a1a' });
defHuman('villager1', { H: '#7a4a2a', C: '#4aa05a', P: '#3a5a8a', B: '#3a2a1a' });
defHuman('villager2', { H: '#2a2a2a', C: '#c08040', P: '#5a4030', B: '#3a2a1a' });
defHuman('child', { H: '#e07040', C: '#5ab0e0', P: '#3a5a8a', B: '#3a2a1a' });

function defMonster(name, rows, pal) { SPRITES[name] = { rows, pal: Object.assign({}, BASE_PAL, pal) }; }

const SLIME = [
  '................',
  '................',
  '................',
  '................',
  '................',
  '.......kk.......',
  '.....kkGGkk.....',
  '....kGGGGGGk....',
  '...kGwwGGGGGk...',
  '...kGwGGGGGGk...',
  '..kGGGkGGkGGGk..',
  '..kGGGkGGkGGGk..',
  '..kGGGGGGGGGGk..',
  '..kGGGGkkGGGGk..',
  '...kkGGGGGGkk...',
  '.....kkkkkk.....',
];
defMonster('slime', SLIME, { G: '#5ac84a' });
defMonster('slime_blue', SLIME, { G: '#5aa8f0' });

defMonster('bat', [
  '................',
  '................',
  '................',
  '...k........k...',
  '..kGk..kk..kGk..',
  '.kGGGkkGGkkGGGk.',
  'kGGGGGGGGGGGGGGk',
  'kGGkGGrGGrGGkGGk',
  'kGk.kGGGGGGk.kGk',
  'kk...kGwwGk...kk',
  '......kGGk......',
  '.......kk.......',
  '................',
  '................',
  '................',
  '................',
], { G: '#7a4aa8' });

defMonster('goblin', [
  '................',
  '......kkkk......',
  '.k...kGGGGk...k.',
  'kGk.kGGGGGGk.kGk',
  '.kGkGrGGGGrGkGk.',
  '..kGGGGGGGGGGk..',
  '...kGGwkkwGGk...',
  '....kkGGGGkk....',
  '...kCCCCCCCCk...',
  '..kGkCCCCCCkGk..',
  '..kGkCCCCCCkGk..',
  '...kkCCCCCCkk...',
  '....kCCkkCCk....',
  '....kGGkkGGk....',
  '...kGGGkkGGGk...',
  '...kkkkkkkkkk...',
], { G: '#6aa83a', C: '#8a5a2b' });

defMonster('rock', [
  '................',
  '................',
  '................',
  '......kkkk......',
  '....kkGGGGkk....',
  '...kGGGGGGGGk...',
  '..kGGdGGGGdGGk..',
  '..kGGrrGGrrGGk..',
  '.kGGGGGGGGGGGGk.',
  '.kGdGGGkkGGGdGk.',
  '.kGGGGGGGGGGGGk.',
  '..kGGGdGGdGGGk..',
  '..kGGGGGGGGGGk..',
  '...kkGGGGGGkk...',
  '.....kkkkkk.....',
  '................',
], { G: '#9a8e80', d: '#6a6056' });

defMonster('wisp', [
  '................',
  '.......k........',
  '......kGk.......',
  '......kGGk......',
  '.....kGyGk......',
  '....kGyyGGk.....',
  '....kGyyyGk.....',
  '...kGyywwyGk....',
  '...kGywwwwyGk...',
  '...kGywkwkyGk...',
  '...kGyywwyyGk...',
  '....kGyyyyGk....',
  '.....kGGGGk.....',
  '......kkkk......',
  '................',
  '................',
], { G: '#e05a2a' });

defMonster('golem', [
  '....kk....kk....',
  '...kwwk..kwwk...',
  '...kwGkkkkGwk...',
  '..kGGGGGGGGGGk..',
  '.kGGrrGGGGrrGGk.',
  '.kGGrrGGGGrrGGk.',
  '.kGGGGGkkGGGGGk.',
  'kGGkwkwkkwkwkGGk',
  'kGGGkGkGGkGkGGGk',
  'kGdGGGGGGGGGGdGk',
  'kGGdGGGGGGGGdGGk',
  '.kGGGdGGGGdGGGk.',
  '.kGGGGGGGGGGGGk.',
  '..kGGk.kk.kGGk..',
  '.kGGGk....kGGGk.',
  '.kkkkk....kkkkk.',
], { G: '#cfc4a8', d: '#8a7e66' });

const DRAGON = [
  '................',
  '....yy..........',
  '.....yk.kk......',
  '...kkkGkGGk.....',
  '..kGGGGGGGGk....',
  '.kGwkGGGGGGk....',
  'kGGGGGGGGGGGk...',
  'kkkkkGGGGGGGGkk.',
  '...kkdGGGGGGGGGk',
  '....kddGGGkGGGk.',
  '...kGddGGGkkGGk.',
  '...kGGddGGGk.kk.',
  '....kGddGGGGk...',
  '....kGGkkGGGk...',
  '...kGGk..kGGk...',
  '...kkk...kkk....',
];
defMonster('dragon_fire', DRAGON, { G: '#d8462a', d: '#f2a03a' });
defMonster('dragon_thunder', DRAGON, { G: '#e0b828', d: '#fff2a0' });
defMonster('dragon_twin', DRAGON, { G: '#7a3ad8', d: '#e0c0ff' });

// ---------- キャラクター ----------
const CHARS = {
  ren: {
    name: 'レン', sprite: 'ren', dragon: true,
    base: { maxhp: 42, maxmp: 6, atk: 11, def: 7, spd: 8, mag: 5 },
    grow: { maxhp: 9, maxmp: 1.5, atk: 2.4, def: 1.6, spd: 1.1, mag: 1.2 },
    learn: [[4, 'heal']],
  },
  mina: {
    name: 'ミーナ', sprite: 'mina',
    base: { maxhp: 30, maxmp: 22, atk: 6, def: 5, spd: 10, mag: 11 },
    grow: { maxhp: 7, maxmp: 4, atk: 1.2, def: 1.2, spd: 1.4, mag: 2.4 },
    learn: [[1, 'heal'], [1, 'fire'], [3, 'thunder'], [5, 'blizzard'], [6, 'healall']],
  },
  garo: {
    name: 'ガロ', sprite: 'garo',
    base: { maxhp: 56, maxmp: 6, atk: 13, def: 8, spd: 5, mag: 2 },
    grow: { maxhp: 11, maxmp: 1, atk: 2.8, def: 2, spd: 0.8, mag: 0.5 },
    learn: [[1, 'roar'], [3, 'smash'], [6, 'wild']],
  },
};

// target: enemy / enemies / ally / allies / self
const SPELLS = {
  heal:     { name: 'ヒール', mp: 3, type: 'heal', power: 22, target: 'ally', field: true },
  healall:  { name: 'ヒールオール', mp: 9, type: 'heal', power: 18, target: 'allies', field: true },
  fire:     { name: 'ファイア', mp: 3, type: 'mag', power: 14, target: 'enemy', element: 'fire' },
  thunder:  { name: 'サンダー', mp: 5, type: 'mag', power: 22, target: 'enemy', element: 'thunder' },
  blizzard: { name: 'ブリザド', mp: 7, type: 'mag', power: 14, target: 'enemies', element: 'ice' },
  roar:     { name: '咆哮', mp: 2, type: 'buff', stat: 'atk', mult: 1.5, target: 'self' },
  smash:    { name: '岩砕き', mp: 3, type: 'phys', mult: 1.8, pierce: true, target: 'enemy' },
  wild:     { name: '乱れ引っかき', mp: 5, type: 'phys', mult: 0.9, target: 'enemies' },
};

// 竜変身フォーム (遺伝子の組み合わせで解放)
const GENES = {
  fire: { name: '炎の遺伝子' },
  thunder: { name: '雷の遺伝子' },
};
const FORMS = {
  flame:   { name: 'フレイムドラゴン', req: ['fire'], cost: 4, sprite: 'dragon_fire',
             mult: { atk: 1.8, def: 1.4, spd: 1.0, mag: 1.5 },
             breath: { name: 'フレイムブレス', power: 30, element: 'fire' } },
  thunder: { name: 'サンダードラゴン', req: ['thunder'], cost: 5, sprite: 'dragon_thunder',
             mult: { atk: 1.6, def: 1.3, spd: 1.6, mag: 1.8 },
             breath: { name: 'サンダーブレス', power: 36, element: 'thunder' } },
  twin:    { name: 'ツインドラゴン', req: ['fire', 'thunder'], cost: 9, sprite: 'dragon_twin',
             mult: { atk: 2.5, def: 2.0, spd: 1.5, mag: 2.5 },
             breath: { name: 'ツインバースト', power: 60 } },
};

// ---------- アイテム ----------
const ITEMS = {
  herb:    { name: 'やくそう', desc: 'HPを30回復する。', type: 'heal', power: 30, price: 8 },
  potion:  { name: '回復薬', desc: 'HPを90回復する。', type: 'heal', power: 90, price: 30 },
  ether:   { name: '魔法の水', desc: 'MPを15回復する。', type: 'mp', power: 15, price: 40 },
  tear:    { name: '竜の涙', desc: 'APを15回復する。竜の血を持つ者にだけ効く。', type: 'ap', power: 15, price: 50 },
  revive:  { name: '気つけ薬', desc: '戦闘不能から復活させる（HP半分）。', type: 'revive', price: 60 },
  trout:   { name: 'ニジマス', desc: '釣った魚。HPを45回復する。', type: 'heal', power: 45 },
  bigfish: { name: '大ナマズ', desc: '池のヌシ。HPを150回復する。', type: 'heal', power: 150 },
};

const EQUIPS = {
  ironsword: { name: '鉄の剣', who: 'ren', slot: 'weapon', bonus: { atk: 6 }, price: 120 },
  oakstaff:  { name: '樫の杖', who: 'mina', slot: 'weapon', bonus: { atk: 2, mag: 4 }, price: 100 },
  ironclaw:  { name: '鉄の爪', who: 'garo', slot: 'weapon', bonus: { atk: 6 }, price: 120 },
  leather:   { name: '革の鎧', who: 'ren', slot: 'armor', bonus: { def: 4 }, price: 80 },
  silkrobe:  { name: '絹のローブ', who: 'mina', slot: 'armor', bonus: { def: 3, mag: 2 }, price: 80 },
  hidevest:  { name: '獣皮の胴着', who: 'garo', slot: 'armor', bonus: { def: 5 }, price: 90 },
};
const STAT_NAMES = { maxhp: '最大HP', maxmp: '最大MP', atk: '攻撃力', def: '防御力', spd: '素早さ', mag: '魔力' };

const SHOP_LIST = ['herb', 'potion', 'ether', 'tear', 'revive',
  'ironsword', 'leather', 'oakstaff', 'silkrobe', 'ironclaw', 'hidevest'];

// ---------- 敵 ----------
const ENEMIES = {
  slime:    { name: 'スライム', sprite: 'slime', hp: 18, atk: 7, def: 2, spd: 4, mag: 2, exp: 4, gold: 3, weak: ['fire'] },
  bat:      { name: 'オオコウモリ', sprite: 'bat', hp: 14, atk: 8, def: 1, spd: 11, mag: 2, exp: 5, gold: 4, weak: ['thunder'] },
  goblin:   { name: 'ゴブリン', sprite: 'goblin', hp: 28, atk: 11, def: 4, spd: 6, mag: 2, exp: 9, gold: 9,
              skills: [{ name: 'こんぼう', kind: 'phys', mult: 1.4, target: 'one', rate: 0.25 }] },
  bslime:   { name: 'アイススライム', sprite: 'slime_blue', hp: 26, atk: 9, def: 4, spd: 5, mag: 8, exp: 10, gold: 6,
              weak: ['fire'], resist: ['ice'],
              skills: [{ name: 'つららとばし', kind: 'mag', power: 12, element: 'ice', target: 'one', rate: 0.35 }] },
  rockling: { name: 'ロックリング', sprite: 'rock', hp: 38, atk: 13, def: 11, spd: 3, mag: 2, exp: 15, gold: 11,
              weak: ['thunder'], resist: ['fire'] },
  wisp:     { name: 'ウィスプ', sprite: 'wisp', hp: 22, atk: 6, def: 3, spd: 9, mag: 10, exp: 12, gold: 8,
              weak: ['ice'], resist: ['fire'],
              skills: [{ name: 'ほのおのいき', kind: 'mag', power: 10, element: 'fire', target: 'all', rate: 0.4 }] },
  golem:    { name: '竜骨のゴーレム', sprite: 'golem', hp: 650, atk: 18, def: 9, spd: 6, mag: 12, exp: 150, gold: 200,
              boss: true, weak: ['thunder'], resist: ['fire'],
              skills: [{ name: '地鳴り', kind: 'mag', power: 14, target: 'all', rate: 0.3 },
                       { name: '骨砕き', kind: 'phys', mult: 1.7, target: 'one', rate: 0.3 }] },
};

// ---------- マップ ----------
// . 草  , 道  T 木  ~ 水  ^ 山  * 森  = 橋  H 屋根  W 壁  D 扉  E 出口
// F 釣り場  v 村  c 洞窟  # 岩壁  f 洞窟床  R 岩  < 出口(階段)
const MAPS = {
  village: {
    name: 'ルフの村',
    rows: [
      'TTTTTTTTTTTTTTTTTTTT',
      'T........,.........T',
      'T.HHHH...,.HHHHH...T',
      'T.HHHH...,.HHHHH...T',
      'T.WWDW...,.WWDWW...T',
      'T...,,,,,,,,,,,....T',
      'T........,.........T',
      'T........,...~~~~..T',
      'T..HHHH..,..~~~~~~.T',
      'T..HHHH..,.F~~~~~~.T',
      'T..WDWW..,..~~~~~~.T',
      'T...,,,,,,...~~~~..T',
      'T........,.........T',
      'T.T......,......T..T',
      'TTTTTTTTTEETTTTTTTTT',
    ],
  },
  world: {
    name: 'ハルト平原',
    rows: [
      '^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^',
      '^^^^^^^^^^......****...^^^^^^^',
      '^^^^^^^^......******....c^^^^^',
      '^^^^^^^.......******.......^^^',
      '^^^^^.........*****........^^^',
      '^^^^....**...........**....^^^',
      '^^^....****.........****...^^^',
      '^^.....****..........**.....^^',
      '^^......**..................^^',
      '^^~~~~~~~~~~~~=~~~~~~~~~~~~~~~',
      '^^............,............^^^',
      '^^....**......,........**..^^^',
      '^^...****.....,.......****..^^',
      '^^...****.....,........**...^^',
      '^^....**......,.............^^',
      '^^............,...~~~~......^^',
      '^^...,,,,,,,,,,..~~~~~~.....^^',
      '^^...v...........~~~~~......^^',
      '^^...............~~~~.......^^',
      '^^^.......**...............^^^',
      '^^^^^...****.........^^^^^^^^^',
      '^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^',
    ],
    encounters: {
      normal: [['slime'], ['slime', 'slime'], ['bat'], ['bat', 'slime'], ['slime', 'slime', 'bat']],
      forest: [['goblin'], ['goblin', 'bat'], ['slime', 'goblin'], ['bat', 'bat']],
    },
  },
  cave: {
    name: '竜骨の洞窟',
    dark: true,
    rows: [
      '########################',
      '####ffffffffffffffff####',
      '####ffffffffffffffff####',
      '####ffffffffffffffff####',
      '###########f############',
      '###########R############',
      '###########f############',
      '#ffff######f######ffff##',
      '#ffff######f######ffff##',
      '#ffffffffffffffffRffff##',
      '#ffff######f######ffff##',
      '#ffff######f############',
      '###f#######f############',
      '###f#######f#####fffff##',
      '###ffffff##f##ffffffff##',
      '########f##f##f###ffff##',
      '########ffffffff########',
      '###########f############',
      '###########<############',
      '########################',
    ],
    encounters: {
      normal: [['bat', 'bat'], ['bslime'], ['bslime', 'bat'], ['rockling'], ['goblin', 'bslime'],
               ['wisp', 'bat'], ['rockling', 'wisp']],
    },
  },
};

const SOLID = new Set(['T', '~', '^', 'H', 'W', 'D', 'F', '#', 'R', ' ']);
