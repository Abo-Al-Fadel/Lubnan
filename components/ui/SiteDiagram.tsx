'use client';

import { useEffect, useRef } from 'react';

/**
 * Purpose-built line diagrams, one per heritage site.
 *
 * taiko-technical-diagram-scroll: every card gets a drawn diagram rather than
 * an icon from a set — the most expensive and most effective way to avoid the
 * three-icon feature grid. These earn it because the six sites have real
 * measurable structure worth drawing: the trilithon at true relative scale
 * against a human figure, Anjar's street grid, Byblos' occupation strata.
 *
 * Drawn on Canvas rather than as hand-authored SVG path data, and every stroke
 * uses the same weight so the six read as one set.
 */

export type DiagramKind = 'trilithon' | 'grid' | 'strata' | 'causeway' | 'profile' | 'grove';

const W = 480;
const H = 300;

function draw(ctx: CanvasRenderingContext2D, kind: DiagramKind, ink: string, accent: string) {
  ctx.clearRect(0, 0, W, H);
  ctx.strokeStyle = ink;
  ctx.fillStyle = ink;
  ctx.lineWidth = 1;
  ctx.font = '9px ui-monospace, monospace';
  ctx.textBaseline = 'middle';

  const label = (text: string, x: number, y: number) => {
    ctx.globalAlpha = 0.55;
    ctx.fillText(text, x, y);
    ctx.globalAlpha = 1;
  };

  switch (kind) {
    /* Baalbek: the three podium blocks, with a 1.75 m figure at the same
       scale. The point of the drawing is the ratio, so the figure is the
       most important stroke in it. */
    case 'trilithon': {
      const scale = 320 / 19.6; // metres → px, longest block ≈ 19.6 m
      const bh = 4.2 * scale;
      const y = 190;
      let x = 70;
      [19.6, 19.3, 19.0].forEach((len, i) => {
        const bw = (len * scale) / 3;
        ctx.strokeRect(x, y - bh, bw, bh);
        label(`${len} m`, x + 6, y - bh / 2);
        x += bw + 2;
      });
      // human figure at the same scale
      const fh = 1.75 * scale;
      ctx.strokeStyle = accent;
      ctx.beginPath();
      ctx.arc(46, y - fh + 4, 3.2, 0, Math.PI * 2);
      ctx.moveTo(46, y - fh + 8);
      ctx.lineTo(46, y - fh / 2.4);
      ctx.moveTo(42, y - fh + 14);
      ctx.lineTo(50, y - fh + 14);
      ctx.moveTo(46, y - fh / 2.4);
      ctx.lineTo(42, y);
      ctx.moveTo(46, y - fh / 2.4);
      ctx.lineTo(50, y);
      ctx.stroke();
      ctx.strokeStyle = ink;
      ctx.beginPath();
      ctx.moveTo(30, y);
      ctx.lineTo(450, y);
      ctx.stroke();
      label('~800 t each', 70, 220);
      label('1.75 m', 22, y - fh - 12);
      break;
    }

    /* Anjar: the Umayyad grid — two colonnaded streets crossing at right
       angles inside a rectangular wall with corner and interval towers. */
    case 'grid': {
      const x0 = 110;
      const y0 = 40;
      const w = 260;
      const h = 220;
      ctx.strokeRect(x0, y0, w, h);
      ctx.beginPath();
      ctx.moveTo(x0 + w / 2, y0);
      ctx.lineTo(x0 + w / 2, y0 + h);
      ctx.moveTo(x0, y0 + h / 2);
      ctx.lineTo(x0 + w, y0 + h / 2);
      ctx.stroke();
      // tetrapylon at the crossing
      ctx.strokeStyle = accent;
      ctx.strokeRect(x0 + w / 2 - 12, y0 + h / 2 - 12, 24, 24);
      ctx.strokeStyle = ink;
      // towers
      ctx.globalAlpha = 0.6;
      for (let i = 0; i <= 4; i++) {
        for (const [cx, cy] of [
          [x0 + (i * w) / 4, y0],
          [x0 + (i * w) / 4, y0 + h],
        ]) {
          ctx.beginPath();
          ctx.arc(cx, cy, 4, 0, Math.PI * 2);
          ctx.stroke();
        }
      }
      for (let i = 1; i < 4; i++) {
        for (const [cx, cy] of [
          [x0, y0 + (i * h) / 4],
          [x0 + w, y0 + (i * h) / 4],
        ]) {
          ctx.beginPath();
          ctx.arc(cx, cy, 4, 0, Math.PI * 2);
          ctx.stroke();
        }
      }
      ctx.globalAlpha = 1;
      label('cardo', x0 + w / 2 + 6, y0 + 16);
      label('decumanus', x0 + 8, y0 + h / 2 - 10);
      label('tetrapylon', x0 + w / 2 + 18, y0 + h / 2 + 26);
      break;
    }

    /* Byblos: occupation layers in section. Depth is the content. */
    case 'strata': {
      const layers = [
        ['Crusader', 1108],
        ['Roman', -64],
        ['Persian', -539],
        ['Bronze Age', -2000],
        ['Neolithic', -5000],
      ] as const;
      const x0 = 60;
      const w = 340;
      let y = 60;
      layers.forEach(([name, year], i) => {
        const h = 30 + i * 6;
        ctx.globalAlpha = 0.14 + i * 0.03;
        ctx.fillRect(x0, y, w, h);
        ctx.globalAlpha = 1;
        ctx.strokeRect(x0, y, w, h);
        label(name, x0 + w + 8, y + h / 2);
        label(year < 0 ? `${-year} BC` : `${year} AD`, x0 - 44, y + h / 2);
        y += h;
      });
      ctx.strokeStyle = accent;
      ctx.beginPath();
      ctx.moveTo(x0 - 12, 60);
      ctx.lineTo(x0 - 12, y);
      ctx.stroke();
      break;
    }

    /* Tyre: the island, Alexander's mole, and the silt that made it a
       peninsula. Three states of the same coastline. */
    case 'causeway': {
      const yMid = 150;
      ctx.globalAlpha = 0.5;
      ctx.beginPath();
      ctx.moveTo(360, 40);
      ctx.lineTo(360, 260);
      ctx.stroke();
      label('mainland', 366, 50);
      ctx.globalAlpha = 1;
      ctx.beginPath();
      ctx.ellipse(140, yMid, 62, 78, 0, 0, Math.PI * 2);
      ctx.stroke();
      label('island city', 96, yMid);
      // the mole
      ctx.strokeStyle = accent;
      ctx.beginPath();
      ctx.moveTo(202, yMid);
      ctx.lineTo(360, yMid);
      ctx.stroke();
      label('mole, 332 BC', 224, yMid - 12);
      // silt
      ctx.strokeStyle = ink;
      ctx.globalAlpha = 0.35;
      ctx.beginPath();
      ctx.moveTo(202, yMid - 6);
      ctx.bezierCurveTo(260, yMid - 52, 310, yMid - 46, 360, yMid - 40);
      ctx.moveTo(202, yMid + 6);
      ctx.bezierCurveTo(260, yMid + 52, 310, yMid + 46, 360, yMid + 40);
      ctx.stroke();
      ctx.globalAlpha = 1;
      label('silted, now an isthmus', 214, yMid + 62);
      break;
    }

    /* Qadisha: valley section, rim to river, with the hermitages cut into
       the wall at the height they actually sit. */
    case 'profile': {
      const base = 250;
      ctx.beginPath();
      ctx.moveTo(30, 70);
      ctx.lineTo(120, 74);
      ctx.bezierCurveTo(180, 92, 200, 200, 240, base);
      ctx.bezierCurveTo(280, 200, 300, 92, 360, 74);
      ctx.lineTo(450, 70);
      ctx.stroke();
      ctx.globalAlpha = 0.45;
      ctx.beginPath();
      ctx.moveTo(30, 70);
      ctx.lineTo(450, 70);
      ctx.stroke();
      ctx.globalAlpha = 1;
      label('rim · 1,500 m', 34, 60);
      label('river · 500 m', 210, base + 14);
      ctx.strokeStyle = accent;
      [
        [186, 168],
        [214, 202],
        [286, 190],
      ].forEach(([x, y]) => {
        ctx.strokeRect(x - 5, y - 5, 10, 10);
      });
      ctx.strokeStyle = ink;
      label('hermitages', 300, 172);
      break;
    }

    /* The grove: trunk count over time. A decline, drawn honestly. */
    case 'grove': {
      const pts: [number, number][] = [
        [1800, 100],
        [1850, 74],
        [1900, 44],
        [1950, 28],
        [2000, 20],
        [2025, 19],
      ];
      const x = (yr: number) => 60 + ((yr - 1800) / 225) * 350;
      const y = (v: number) => 250 - (v / 100) * 190;
      ctx.globalAlpha = 0.35;
      [0, 25, 50, 75, 100].forEach((v) => {
        ctx.beginPath();
        ctx.moveTo(60, y(v));
        ctx.lineTo(410, y(v));
        ctx.stroke();
      });
      ctx.globalAlpha = 1;
      ctx.strokeStyle = accent;
      ctx.lineWidth = 1.6;
      ctx.beginPath();
      pts.forEach(([yr, v], i) => (i ? ctx.lineTo(x(yr), y(v)) : ctx.moveTo(x(yr), y(v))));
      ctx.stroke();
      ctx.lineWidth = 1;
      ctx.strokeStyle = ink;
      pts.forEach(([yr, v]) => {
        ctx.beginPath();
        ctx.arc(x(yr), y(v), 2.4, 0, Math.PI * 2);
        ctx.fill();
      });
      label('1800', 50, 266);
      label('2025', 396, 266);
      label('relative grove extent', 66, 40);
      break;
    }
  }

  /* Registration crosshairs at the frame corners — the vocabulary that ties
     the six diagrams into one drawing set. */
  ctx.globalAlpha = 0.4;
  [
    [14, 14],
    [W - 14, 14],
    [14, H - 14],
    [W - 14, H - 14],
  ].forEach(([cx, cy]) => {
    ctx.beginPath();
    ctx.moveTo(cx - 5, cy);
    ctx.lineTo(cx + 5, cy);
    ctx.moveTo(cx, cy - 5);
    ctx.lineTo(cx, cy + 5);
    ctx.stroke();
  });
  ctx.globalAlpha = 1;
}

export function SiteDiagram({ kind, className = '' }: { kind: DiagramKind; className?: string }) {
  const ref = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = ref.current;
    if (!canvas) return;

    const render = () => {
      const dpr = window.devicePixelRatio || 1;
      canvas.width = W * dpr;
      canvas.height = H * dpr;
      const ctx = canvas.getContext('2d');
      if (!ctx) return;
      ctx.scale(dpr, dpr);
      /* Read the palette off the document so the diagrams follow the theme
         toggle instead of pinning their own colours. */
      const cs = getComputedStyle(canvas);
      draw(ctx, kind, cs.color, cs.getPropertyValue('--accent').trim() || cs.color);
    };

    render();
    const mo = new MutationObserver(render);
    mo.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
    window.addEventListener('resize', render);
    return () => {
      mo.disconnect();
      window.removeEventListener('resize', render);
    };
  }, [kind]);

  return (
    <canvas
      ref={ref}
      role="img"
      aria-label={`Diagram: ${kind}`}
      className={`w-full text-ink ${className}`}
      style={{ aspectRatio: `${W} / ${H}` }}
    />
  );
}
