import { readFileSync, writeFileSync } from 'node:fs';

/**
 * Remove em dashes from user-facing copy.
 *
 * Not a blind swap for a comma: an em dash usually joins two independent
 * clauses, and a comma between those is a splice. So when what follows is a
 * conjunction or a relative pronoun the clause is subordinate and a comma is
 * correct; otherwise the dash was standing in for a full stop and that is what
 * it becomes, with the next word capitalised.
 *
 * Block comments are left alone. This is about the website's text.
 */
const FILES = process.argv.slice(2);

const SUBORDINATE =
  /^(which|and|so|but|or|because|though|although|while|whose|who|that|then|yet|for|with|not|until|unless|since|after|before|as)$/i;

let total = 0;

for (const file of FILES) {
  const src = readFileSync(file, 'utf8');

  // Protect block comments so prose inside them is untouched. The token is
  // deliberately unlike anything that appears in source or copy.
  const comments = [];
  let work = src.replace(/\/\*[\s\S]*?\*\//g, (m) => {
    comments.push(m);
    return `@@CMT${comments.length - 1}@@`;
  });

  let n = 0;
  // Capture the whole following word so the decision can be made on it, and
  // so it can be re-emitted capitalised when the dash becomes a full stop.
  work = work.replace(/\s*—\s*([A-Za-zÀ-ɏ’']+|\S)/g, (_m, word) => {
    n++;
    if (SUBORDINATE.test(word)) return `, ${word}`;
    if (/^[a-z]/.test(word)) return `. ${word[0].toUpperCase()}${word.slice(1)}`;
    return `. ${word}`;
  });

  /* Tidy only inside quoted strings. Applied to the whole file, `\.\s*\.`
     matches the first two dots of a spread operator and rewrites `...s` to
     `..s` — which is a syntax error, and is exactly what happened the first
     time this ran. */
  work = work.replace(/'([^'\\\n]*(?:\\.[^'\\\n]*)*)'/g, (m, body) => {
    const cleaned = body
      .replace(/,\s*\./g, '.')
      .replace(/\.\s+\./g, '.')
      .replace(/\.\s+([,;:!?])/g, '$1');
    return `'${cleaned}'`;
  });

  work = work.replace(/@@CMT(\d+)@@/g, (_m, i) => comments[Number(i)]);

  if (work !== src) {
    writeFileSync(file, work);
    console.log(`${String(n).padStart(3)}  ${file}`);
    total += n;
  }
}

console.log(`\n${total} replaced`);
