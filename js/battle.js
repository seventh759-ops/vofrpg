'use strict';
// 戦闘シーン

const alive = b => b.ref.hp > 0;

function bStat(b, k) {
  let v = b.isEnemy ? b.data[k] : stat(b.ref, k);
  if (b.form) v *= FORMS[b.form].mult[k] || 1;
  if (b.buff[k]) v *= b.buff[k];
  return v;
}
const bMaxHp = b => (b.isEnemy ? b.data.hp : stat(b.ref, 'maxhp'));

function elemMult(t, el) {
  if (!el || !t.isEnemy) return 1;
  if ((t.data.weak || []).includes(el)) return 1.5;
  if ((t.data.resist || []).includes(el)) return 0.5;
  return 1;
}

function physDmg(u, t, mult = 1, pierce = false) {
  const atk = bStat(u, 'atk') * mult, def = bStat(t, 'def') * (pierce ? 0.4 : 1);
  let d = (atk * 1.5 - def * 0.75) * (0.85 + Math.random() * 0.3);
  if (t.defend) d /= 2;
  return Math.max(1, Math.round(d));
}

function magDmg(u, t, power) {
  let d = (power + bStat(u, 'mag') * 1.5 - bStat(t, 'mag') * 0.4) * (0.9 + Math.random() * 0.2);
  if (t.defend) d /= 2;
  return Math.max(1, Math.round(d));
}

function healAmount(mag, spell) {
  return Math.round((spell.power + mag * 1.5) * (0.9 + Math.random() * 0.2));
}

function availableForms() {
  return Object.keys(FORMS).filter(f => FORMS[f].req.every(g => G.genes.includes(g)));
}

class Battle {
  constructor(ids, opts = {}) {
    this.opts = opts;
    this.bg = G.map === 'cave' ? 'cave' : 'field';
    const counts = {}, seen = {};
    ids.forEach(id => { counts[id] = (counts[id] || 0) + 1; });
    this.enemies = ids.map(id => {
      const d = ENEMIES[id];
      let name = d.name;
      if (counts[id] > 1) { seen[id] = (seen[id] || 0) + 1; name += 'ABC'[seen[id] - 1]; }
      return { isEnemy: true, id, data: d, name, ref: { hp: d.hp }, buff: {}, flash: 0, deadT: 0 };
    });
    this.party = partyChars().map(c => ({ isEnemy: false, ref: c, name: c.name, buff: {}, form: null, flash: 0, defend: false }));
    this.popups = [];
    this.actions = [];
    this.queue = [];
    this.active = null;
    this.over = false;
    this.glow = 0;
    const intro = opts.boss ? `${this.enemies[0].name}が 立ちはだかった！`
      : ids.length > 1 ? '魔物の群れが あらわれた！' : `${this.enemies[0].name}が あらわれた！`;
    this.say([intro], () => this.startCommands());
  }

  // ---------- メッセージ ----------
  say(msgs, next) {
    this.msgs = msgs.slice();
    this.msgNext = next;
    this.msgT = 0;
    this.phase = 'msg';
    if (!this.msgs.length) { this.msgs = null; next(); }
  }
  updateMsg() {
    this.msgT++;
    if (this.msgT > 70 || (this.msgT > 8 && Input.pressed.ok)) {
      this.msgs.shift();
      this.msgT = 0;
      if (!this.msgs.length) {
        const n = this.msgNext;
        this.msgs = null;
        this.msgNext = null;
        n();
      }
    }
  }

  // ---------- コマンド入力 ----------
  startCommands() {
    this.actions = [];
    const first = this.party.findIndex(alive);
    this.openCommand(first);
  }
  openCommand(i) {
    this.ci = i;
    this.phase = 'cmd';
    this.mode = 'main';
    this.cmdMenu = new Menu(this.commandsFor(this.party[i]));
  }
  commandsFor(b) {
    const c = b.ref;
    if (b.form) {
      return [{ label: 'たたかう', v: 'attack' }, { label: 'ブレス', v: 'breath' },
              { label: 'もどる', v: 'revert' }, { label: 'ぼうぎょ', v: 'defend' }];
    }
    const items = [{ label: 'たたかう', v: 'attack' }];
    if (CHARS[c.id].dragon) items.push({ label: '竜変身', v: 'dragon', disabled: !availableForms().length });
    if (spellsOf(c).length) items.push({ label: c.id === 'garo' ? 'わざ' : 'まほう', v: 'spell' });
    items.push({ label: 'アイテム', v: 'item', disabled: !Object.keys(G.items).length });
    items.push({ label: 'ぼうぎょ', v: 'defend' });
    items.push({ label: 'にげる', v: 'escape', disabled: !!this.opts.boss });
    return items;
  }
  commit(a) {
    a.actor = this.party[this.ci];
    this.actions.push(a);
    let n = this.ci + 1;
    while (n < this.party.length && !alive(this.party[n])) n++;
    if (n >= this.party.length) this.execRound();
    else this.openCommand(n);
  }
  beginTarget(a, side, includeDead = false) {
    this.pending = a;
    this.prevMode = this.mode;
    this.mode = 'target';
    this.tside = side;
    this.tdead = includeDead;
    this.ti = side === 'ally' ? Math.max(0, this.targets().indexOf(this.party[this.ci])) : 0;
  }
  targets() {
    if (this.tside === 'enemy') return this.enemies.filter(alive);
    return this.party.filter(b => this.tdead || alive(b));
  }
  updateCmd() {
    const b = this.party[this.ci];
    if (this.mode === 'main') {
      const r = this.cmdMenu.update();
      if (r === 'cancel') {
        if (this.actions.length) this.openCommand(this.party.indexOf(this.actions.pop().actor));
        return;
      }
      if (r !== 'ok') return;
      const v = this.cmdMenu.cur.v;
      if (v === 'attack') this.beginTarget({ kind: 'attack' }, 'enemy');
      else if (v === 'defend' || v === 'escape' || v === 'revert' || v === 'breath') this.commit({ kind: v });
      else if (v === 'dragon') {
        this.mode = 'form';
        this.subMenu = new Menu(availableForms().map(f => ({
          label: FORMS[f].name, right: `AP${FORMS[f].cost}`, v: f, disabled: b.ref.ap < FORMS[f].cost,
        })), { rows: 4 });
      } else if (v === 'spell') {
        this.mode = 'spell';
        this.subMenu = new Menu(spellsOf(b.ref).map(s => ({
          label: SPELLS[s].name, right: `MP${SPELLS[s].mp}`, v: s, disabled: b.ref.mp < SPELLS[s].mp,
        })), { rows: 4 });
      } else if (v === 'item') {
        this.mode = 'item';
        this.subMenu = new Menu(Object.keys(G.items).map(id => ({
          label: ITEMS[id].name, right: `×${G.items[id]}`, v: id,
        })), { rows: 4 });
      }
    } else if (this.mode === 'form' || this.mode === 'spell' || this.mode === 'item') {
      const r = this.subMenu.update();
      if (r === 'cancel') { this.mode = 'main'; return; }
      if (r !== 'ok') return;
      const v = this.subMenu.cur.v;
      if (this.mode === 'form') this.commit({ kind: 'transform', form: v });
      else if (this.mode === 'spell') {
        const t = SPELLS[v].target;
        const a = { kind: 'spell', spell: v };
        if (t === 'enemy') this.beginTarget(a, 'enemy');
        else if (t === 'ally') this.beginTarget(a, 'ally');
        else this.commit(a);
      } else {
        this.beginTarget({ kind: 'item', item: v }, 'ally', true);
      }
    } else if (this.mode === 'target') {
      const list = this.targets();
      if (Input.rep('left') || Input.rep('up')) { this.ti = (this.ti - 1 + list.length) % list.length; Sound.play('cursor'); }
      if (Input.rep('right') || Input.rep('down')) { this.ti = (this.ti + 1) % list.length; Sound.play('cursor'); }
      if (Input.pressed.ok) {
        Sound.play('ok');
        this.commit(Object.assign(this.pending, { target: list[this.ti] }));
      } else if (Input.pressed.cancel) {
        Sound.play('cancel');
        this.mode = this.prevMode;
      }
    }
  }

  // ---------- 実行 ----------
  enemyAction(e) {
    const sk = (e.data.skills || []).find(s => Math.random() < s.rate);
    return { actor: e, kind: sk ? 'eskill' : 'attack', skill: sk, target: pick(this.party.filter(alive)) };
  }
  execRound() {
    this.phase = 'exec';
    for (const e of this.enemies) if (alive(e)) this.actions.push(this.enemyAction(e));
    this.actions.forEach(a => {
      a.speed = a.kind === 'defend' ? 9999 : bStat(a.actor, 'spd') * (0.8 + Math.random() * 0.4);
    });
    this.queue = this.actions.sort((p, q) => q.speed - p.speed);
    this.actions = [];
    this.execNext();
  }
  execNext() {
    if (this.checkEnd()) return;
    const a = this.queue.shift();
    if (!a) { this.endRound(); return; }
    if (!alive(a.actor)) { this.execNext(); return; }
    this.active = a.actor;
    const msgs = this.perform(a);
    this.say(msgs, () => { this.active = null; this.execNext(); });
  }
  // 単体対象が倒れていたら同じ側の別の生存者に
  fix(t, enemySide) {
    if (t && alive(t)) return t;
    const list = (enemySide ? this.enemies : this.party).filter(alive);
    return list.length ? pick(list) : null;
  }
  popup(b, text, color) {
    const p = this.pos(b);
    this.popups.push({ x: p.x + p.s / 2, y: p.y + p.h / 3, text: String(text), color, t: 0 });
  }
  hit(t, dmg, after) {
    t.ref.hp = Math.max(0, t.ref.hp - dmg);
    t.flash = 18;
    this.popup(t, dmg, '#fff');
    if (t.ref.hp <= 0) {
      if (t.isEnemy) { after.push(`${t.name}を たおした！`); }
      else { t.form = null; t.buff = {}; after.push(`${t.name}は たおれた…`); }
    }
  }
  elemNote(t, el) {
    const m = elemMult(t, el);
    return m > 1 ? ' 弱点だ！' : m < 1 ? ' 効きがわるい…' : '';
  }
  // 複数対象のダメージ処理。dmgFn(t) でダメージを返す
  multiHit(ts, head, dmgFn, el) {
    const after = [];
    if (ts.length === 1) {
      const t = ts[0];
      const dmg = dmgFn(t);
      this.hit(t, dmg, after);
      return [`${head}\n${t.name}に ${dmg}のダメージ！${this.elemNote(t, el)}`, ...after];
    }
    ts.forEach(t => this.hit(t, dmgFn(t), after));
    return [`${head}\n${ts[0].isEnemy ? '敵' : 'みんな'}に ダメージ！`, ...after];
  }
  perform(a) {
    const u = a.actor;
    switch (a.kind) {
      case 'attack': {
        const t = this.fix(a.target, !u.isEnemy);
        if (!t) return [];
        const crit = Math.random() < 1 / 16;
        Sound.play(crit ? 'crit' : 'hit');
        const head = `${u.name}の こうげき！` + (crit ? ' 会心の一撃！' : '');
        return this.multiHit([t], head, x => physDmg(u, x, crit ? 1.6 : 1));
      }
      case 'eskill': {
        const s = a.skill;
        const ts = s.target === 'all' ? this.party.filter(alive) : [this.fix(a.target, false)].filter(Boolean);
        Sound.play(s.kind === 'mag' ? 'magic' : 'crit');
        return this.multiHit(ts, `${u.name}の ${s.name}！`,
          t => (s.kind === 'mag' ? magDmg(u, t, s.power) : physDmg(u, t, s.mult)));
      }
      case 'spell': {
        const s = SPELLS[a.spell];
        const head = `${u.name}の ${s.name}！`;
        if (u.ref.mp < s.mp) return [`${head}\nしかし MPが たりない！`];
        u.ref.mp -= s.mp;
        if (s.type === 'heal') {
          Sound.play('heal');
          const ts = s.target === 'allies' ? this.party.filter(alive) : [a.target].filter(t => t && alive(t));
          if (!ts.length) return [`${head}\nしかし 効果がなかった。`];
          let last = 0;
          ts.forEach(t => {
            const before = t.ref.hp;
            t.ref.hp = Math.min(bMaxHp(t), t.ref.hp + healAmount(bStat(u, 'mag'), s));
            last = t.ref.hp - before;
            this.popup(t, last, '#8f8');
          });
          return [ts.length === 1 ? `${head}\n${ts[0].name}の HPが ${last} 回復した！` : `${head}\nみんなの HPが 回復した！`];
        }
        if (s.type === 'buff') {
          Sound.play('magic');
          u.buff[s.stat] = s.mult;
          return [`${head}\n${u.name}の ${STAT_NAMES[s.stat]}が あがった！`];
        }
        const ts = s.target === 'enemies' ? this.enemies.filter(alive) : [this.fix(a.target, true)].filter(Boolean);
        Sound.play(s.type === 'mag' ? 'magic' : 'crit');
        return this.multiHit(ts, head, t => {
          const d = s.type === 'mag' ? magDmg(u, t, s.power) : physDmg(u, t, s.mult, s.pierce);
          return Math.max(1, Math.round(d * elemMult(t, s.element)));
        }, s.element);
      }
      case 'item': {
        const it = ITEMS[a.item];
        if (!G.items[a.item]) return [`${u.name}は ${it.name}を 使おうとした…\nしかし もう残っていない！`];
        const r = useItemOn(a.item, a.target.ref);
        if (!r) return [`${u.name}は ${it.name}を 使った！\nしかし 効果がなかった。`];
        removeItem(a.item);
        Sound.play('heal');
        if (r.amount) this.popup(a.target, r.amount, it.type === 'heal' ? '#8f8' : '#8cf');
        return [`${u.name}は ${it.name}を 使った！\n${r.msg}`];
      }
      case 'defend':
        u.defend = true;
        return [`${u.name}は 身を守っている。`];
      case 'transform': {
        const f = FORMS[a.form];
        if (u.ref.ap < f.cost) return [`${u.name}は 竜の力を呼び覚まそうとした…\nしかし APが たりない！`];
        u.ref.ap -= f.cost;
        u.form = a.form;
        u.formFresh = true;
        u.flash = 30;
        this.glow = 40;
        Sound.play('dragon');
        return [`${u.name}の 体が 光につつまれる……！\n${f.name}に 変身した！`];
      }
      case 'revert':
        u.form = null;
        return [`${u.name}は 元の姿に もどった。`];
      case 'breath': {
        const f = FORMS[u.form];
        if (!f) return [];
        const br = f.breath;
        this.glow = 30;
        Sound.play('dragon');
        return this.multiHit(this.enemies.filter(alive), `${u.name}の ${br.name}！`, t => {
          const d = (br.power + bStat(u, 'mag') * 2) * (0.9 + Math.random() * 0.2);
          return Math.max(1, Math.round(d * elemMult(t, br.element)));
        }, br.element);
      }
      case 'escape':
        if (Math.random() < 0.6) { this.escaped = true; return ['うまく 逃げきった！']; }
        return ['にげだした！\nしかし 回りこまれてしまった！'];
    }
    return [];
  }
  endRound() {
    const msgs = [];
    for (const b of this.party) {
      if (!b.form) continue;
      if (b.formFresh) { b.formFresh = false; continue; }
      const cost = FORMS[b.form].cost;
      if (b.ref.ap >= cost) b.ref.ap -= cost;
      else { b.form = null; msgs.push(`${b.name}は APが尽きて 元の姿に もどった…`); }
    }
    this.party.forEach(b => { b.defend = false; });
    this.say(msgs, () => { if (!this.checkEnd()) this.startCommands(); });
  }
  checkEnd() {
    if (this.over) return true;
    if (this.escaped) { this.over = true; this.finish('escape'); return true; }
    if (this.enemies.every(e => !alive(e))) { this.over = true; this.victory(); return true; }
    if (this.party.every(b => !alive(b))) { this.over = true; this.defeat(); return true; }
    return false;
  }
  victory() {
    let exp = 0, gold = 0;
    this.enemies.forEach(e => { exp += e.data.exp; gold += e.data.gold; });
    G.gold += gold;
    Sound.play('victory');
    const msgs = ['魔物を やっつけた！', `${exp}の 経験値と ${gold}Gを 手に入れた！`];
    this.party.filter(alive).forEach(b => msgs.push(...giveExp(b.ref, exp)));
    this.party.forEach(b => {
      b.form = null;
      if (CHARS[b.ref.id].dragon) b.ref.ap = Math.min(maxAp(b.ref), b.ref.ap + 3);
    });
    this.say(msgs, () => this.finish('win'));
  }
  defeat() {
    this.say(['レンたちは 全滅してしまった……'], () => this.finish('lose'));
  }
  finish(result) {
    Game.push(new Fade(() => {
      Game.remove(this);
      if (result === 'lose') Game.replace(new GameOver());
      else if (result === 'win' && this.opts.onWin) this.opts.onWin();
    }, 20));
  }

  update() {
    this.popups.forEach(p => p.t++);
    this.popups = this.popups.filter(p => p.t < 50);
    [...this.enemies, ...this.party].forEach(b => { if (b.flash > 0) b.flash--; });
    this.enemies.forEach(e => { if (!alive(e)) e.deadT++; });
    if (this.glow > 0) this.glow--;
    if (this.phase === 'msg') this.updateMsg();
    else if (this.phase === 'cmd') this.updateCmd();
  }

  // ---------- 描画 (クォータービュー) ----------
  // 戦場は 9x9 マス。整数座標がマスの中心。敵は左上、味方は右下に並ぶ
  iso(gx, gy) { return { x: 320 + (gx - gy) * 32, y: 200 + (gx + gy - 8) * 16 }; }
  slot(n, i, gx) { return [gx, n === 1 ? 4 : n === 2 ? [3, 5][i] : [2, 4, 6][i]]; }
  pos(b) {
    let foot, scale;
    if (b.isEnemy) {
      const [gx, gy] = b.data.boss ? [1.6, 4] : this.slot(this.enemies.length, this.enemies.indexOf(b), 2);
      foot = this.iso(gx, gy);
      scale = b.data.boss ? 6 : 4;
      if (this.active === b) { foot.x += 14; foot.y += 7; }
    } else {
      foot = this.iso(...this.slot(3, this.party.indexOf(b), 6));
      scale = b.form ? 4 : 2;
      if (this.active === b || (this.phase === 'cmd' && this.party[this.ci] === b)) { foot.x -= 20; foot.y -= 10; }
    }
    const { w, h } = spriteSize(this.spriteOf(b), scale);
    return { x: foot.x - w / 2, y: foot.y - h + scale * 2, s: w, h, scale, fx: foot.x, fy: foot.y };
  }
  // 味方は敵の方を向いた後ろ姿
  spriteOf(b) {
    if (b.isEnemy) return b.data.sprite;
    return b.form ? FORMS[b.form].sprite : CHARS[b.ref.id].sprite + '_back';
  }
  drawBackground() {
    const cave = this.bg === 'cave';
    const g = ctx.createLinearGradient(0, 0, 0, 340);
    if (cave) { g.addColorStop(0, '#0c0810'); g.addColorStop(1, '#2a2030'); }
    else { g.addColorStop(0, '#4a7ad0'); g.addColorStop(0.3, '#b0d8f4'); g.addColorStop(0.31, '#4f8f40'); g.addColorStop(1, '#2e6a2a'); }
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, W, 340);
    const ground = cave ? 'f' : '.';
    const f = (Game.frame >> 5) % 4;
    for (let gy = 0; gy <= 8; gy++) {
      for (let gx = 0; gx <= 8; gx++) {
        const c = this.iso(gx, gy);
        drawIsoGround(ground, c.x, c.y - 16, f);
      }
    }
    // 奥の縁に木や岩壁を並べる
    for (let k = -1; k <= 8; k++) {
      for (const [gx, gy] of [[-1, k], [k, -1]]) {
        const c = this.iso(gx, gy);
        if (cave) drawBlock(c.x, c.y - 16, 40, faceTex('cavetop'), faceTex('cave'), faceTex('cave'));
        else if ((k + 8) % 2 === 0) drawIsoTree(c.x, c.y);
      }
    }
  }
  draw() {
    ctx.fillStyle = '#000';
    ctx.fillRect(0, 0, W, H);
    this.drawBackground();
    if (this.glow > 0) {
      ctx.fillStyle = `rgba(255,240,200,${this.glow / 80})`;
      ctx.fillRect(0, 0, W, 340);
    }
    // 敵と味方を奥から順に。味方は敵の方を向いた後ろ姿
    const list = [];
    for (const e of this.enemies) {
      if (!alive(e) && e.deadT > 24) continue;
      const p = this.pos(e);
      list.push({ y: p.fy, fn: () => drawIsoChar(e.data.sprite, p.fx, p.fy, p.scale, {
        white: e.flash > 0 && (e.flash >> 1) % 2, alpha: alive(e) ? 1 : 1 - e.deadT / 24,
      }) });
    }
    for (const b of this.party) {
      const p = this.pos(b);
      const sprite = this.spriteOf(b);
      list.push({ y: p.fy, fn: () => {
        drawIsoChar(sprite, p.fx, p.fy, p.scale, { white: b.flash > 0 && (b.flash >> 1) % 2, alpha: alive(b) ? 1 : 0.35 });
        if (b.defend) drawText('盾', p.x - 4, p.y, '#9cf', 16);
      } });
    }
    list.sort((a, b) => a.y - b.y).forEach(d => d.fn());
    // ダメージ表示
    for (const p of this.popups) {
      drawText(p.text, p.x, p.y - Math.min(p.t, 20) * 1.2, p.color, 24, 'center');
    }
    // ターゲットカーソル
    if (this.phase === 'cmd' && this.mode === 'target') {
      const list = this.targets();
      const t = list[this.ti];
      if (t) {
        const p = this.pos(t);
        const bob = (Game.frame >> 3) % 2 * 3;
        if (t.isEnemy) drawText('▼', p.x + p.s / 2, p.y - 26 + bob, '#ffe080', 22, 'center');
        else drawText('▶', p.x - 28 + bob, p.y + p.h / 2 - 12, '#ffe080', 22);
        drawWindow(8, 8, 624, 52);
        const hp = t.isEnemy ? '' : `  HP ${t.ref.hp}/${bMaxHp(t)}`;
        drawText(t.name + hp, 28, 22, '#fff', 20);
      }
    }
    // メッセージ
    if (this.phase === 'msg' && this.msgs && this.msgs.length) {
      drawWindow(8, 8, 624, 76);
      wrapText(this.msgs[0], 588, 20).slice(0, 2).forEach((l, i) => drawText(l, 26, 20 + i * 28, '#fff', 20));
    }
    // ステータス
    drawWindow(216, 340, 416, 132);
    this.party.forEach((b, i) => {
      const y = 352 + i * 38, c = b.ref;
      const cur = this.phase === 'cmd' && this.ci === i;
      const col = !alive(b) ? '#f66' : cur ? '#ffe080' : '#fff';
      drawText(b.form ? '竜' : '', 228, y + 2, '#f84', 16);
      drawText(c.name, 248, y, col, 20);
      drawText(`HP${String(c.hp).padStart(4)}/${stat(c, 'maxhp')}`, 340, y, col, 18);
      drawBar(340, y + 22, 120, 5, c.hp / stat(c, 'maxhp'), '#5c5');
      drawText(`MP${String(c.mp).padStart(3)}`, 476, y, col, 18);
      if (CHARS[c.id].dragon) drawText(`AP${String(c.ap).padStart(3)}`, 556, y, '#fc8', 18);
    });
    // コマンド
    drawWindow(8, 340, 200, 132);
    if (this.phase === 'cmd') {
      this.cmdMenu.rows = 4;
      this.cmdMenu.draw(20, 352, 170, { lineH: 28, size: 18, active: this.mode === 'main' });
      if (this.mode === 'form' || this.mode === 'spell' || this.mode === 'item') {
        drawWindow(8, 196, 330, 144);
        this.subMenu.draw(22, 212, 290, { lineH: 30, size: 19 });
        const cur = this.subMenu.cur;
        if (cur && this.mode === 'item') {
          drawWindow(8, 8, 624, 52);
          drawText(ITEMS[cur.v].desc, 28, 22, '#fff', 18);
        }
      }
      if (this.mode === 'main' || this.mode === 'form' || this.mode === 'spell' || this.mode === 'item') {
        if (this.mode === 'main') {
          drawWindow(8, 8, 624, 52);
          drawText(`${this.party[this.ci].name}は どうする？`, 28, 22, '#fff', 20);
        }
      }
    }
  }
}
