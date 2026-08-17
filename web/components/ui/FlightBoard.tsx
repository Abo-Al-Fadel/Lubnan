'use client';

import { useEffect, useMemo, useState } from 'react';
import { useSite } from '@/lib/site-state';
import {
  AIRPORT,
  FALLBACK_ARRIVALS,
  FALLBACK_DEPARTURES,
  fetchFlights,
  type Flight,
  type FlightStatus,
} from '@/lib/flights';

/**
 * The departures board.
 *
 * Status is carried by shape as well as by colour — a dot, a word and, when a
 * flight is late, the number of minutes — because a board that encodes state
 * only in colour is unreadable to a large minority of the people reading it.
 *
 * Times are tabular-lined so the column reads as a column. Sorting is by
 * scheduled time, not by status, because that is the order a traveller scans.
 */
const STATUS_TOKEN: Record<FlightStatus, { dot: string; key: string }> = {
  'on-time': { dot: 'var(--ink-dim)', key: 'flight.onTime' },
  boarding: { dot: 'var(--accent)', key: 'flight.boarding' },
  delayed: { dot: 'var(--status-warn)', key: 'flight.delayed' },
  landed: { dot: 'var(--ink-faint)', key: 'flight.landed' },
  departed: { dot: 'var(--ink-faint)', key: 'flight.departed' },
  cancelled: { dot: 'var(--status-warn)', key: 'flight.cancelled' },
};

export function FlightBoard() {
  const { tr } = useSite();
  const [mode, setMode] = useState<'departures' | 'arrivals'>('departures');
  const [query, setQuery] = useState('');
  const [live, setLive] = useState(false);
  const [arrivals, setArrivals] = useState<Flight[]>(FALLBACK_ARRIVALS);
  const [departures, setDepartures] = useState<Flight[]>(FALLBACK_DEPARTURES);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      const board = await fetchFlights();
      if (cancelled) return;
      setLive(board.live);
      setArrivals(board.arrivals);
      setDepartures(board.departures);
    };
    void load();
    const id = window.setInterval(() => void load(), 180_000);
    return () => {
      cancelled = true;
      window.clearInterval(id);
    };
  }, []);

  const rows = useMemo(() => {
    const source = mode === 'departures' ? departures : arrivals;
    const q = query.trim().toLowerCase();
    const filtered = q
      ? source.filter((f) =>
          `${f.code} ${f.airline} ${f.city} ${f.iata ?? ''} ${f.country}`.toLowerCase().includes(q),
        )
      : source;
    return [...filtered].sort((a, b) => a.time.localeCompare(b.time));
  }, [mode, query, arrivals, departures]);

  return (
    <div className="border border-ink-ghost bg-ground">
      <div className="flex flex-wrap items-center justify-between gap-4 border-b border-ink-ghost p-4 md:p-6">
        <div>
          <p className="micro text-ink-dim">
            {AIRPORT.iata} · {AIRPORT.icao} · {AIRPORT.distanceKm} km {tr('flight.fromCity')}
            {live ? ` · ${tr('flight.live')}` : ''}
          </p>
          <p className="mt-2 font-display text-lg uppercase tracking-wide text-ink md:text-xl">
            {AIRPORT.name}
          </p>
        </div>

        <div className="flex items-center rounded-full border border-ink-ghost p-0.5">
          {(['departures', 'arrivals'] as const).map((m) => (
            <button
              key={m}
              type="button"
              onClick={() => setMode(m)}
              aria-pressed={mode === m}
              className={`micro flex min-h-[40px] items-center rounded-full px-4 transition-colors ${
                mode === m
                  ? 'bg-[color:var(--nav-accent)] text-[color:var(--nav-accent-ink)]'
                  : 'text-ink-dim hover:text-ink'
              }`}
            >
              {tr(`flight.${m}`)}
            </button>
          ))}
        </div>
      </div>

      <div className="border-b border-ink-ghost p-4 md:px-6">
        <label htmlFor="flight-q" className="sr-only">
          {tr('flight.filter')}
        </label>
        <input
          id="flight-q"
          type="search"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder={tr('flight.filterHint')}
          className="w-full border-0 border-b border-ink-ghost bg-transparent px-0 py-2 text-sm text-ink outline-none transition-colors placeholder:text-ink-dim focus:border-accent"
        />
      </div>

      <div className="overflow-x-auto">
        <table className="w-full min-w-[40rem] border-collapse text-left">
          <caption className="sr-only">
            {tr(`flight.${mode}`)} — {AIRPORT.name}
          </caption>
          <thead>
            <tr className="border-b border-ink-ghost">
              {['flight.colTime', 'flight.colFlight', 'flight.colCity', 'flight.colGate', 'flight.colStatus'].map(
                (k) => (
                  <th key={k} scope="col" className="micro px-4 py-3 font-normal text-ink-dim md:px-6">
                    {tr(k)}
                  </th>
                ),
              )}
            </tr>
          </thead>
          <tbody>
            {rows.map((f) => (
              <Row key={`${f.code}-${f.time}-${f.city}`} flight={f} />
            ))}
          </tbody>
        </table>
      </div>

      {rows.length === 0 ? (
        <p className="px-4 py-8 text-center text-sm text-ink-dim md:px-6">{tr('flight.none')}</p>
      ) : null}

      <p className="micro border-t border-ink-ghost px-4 py-4 leading-[1.7] text-ink-dim md:px-6">
        {live ? tr('flight.disclaimerLive') : tr('flight.disclaimer')}
      </p>
    </div>
  );
}

function Row({ flight: f }: { flight: Flight }) {
  const { tr } = useSite();
  const token = STATUS_TOKEN[f.status] ?? STATUS_TOKEN['on-time'];
  const past = f.status === 'landed' || f.status === 'departed' || f.status === 'cancelled';
  const stand = [f.terminal, f.gate].filter((part) => part && part !== '—').join(' · ') || '—';

  return (
    <tr className={`border-b border-ink-ghost last:border-b-0 ${past ? 'opacity-55' : ''}`}>
      <td className="figures px-4 py-3.5 text-sm text-ink md:px-6">
        {f.time}
        {f.delay > 0 ? (
          <span className="micro ms-2 text-[color:var(--status-warn)]">+{f.delay}</span>
        ) : null}
      </td>
      <td className="px-4 py-3.5 md:px-6">
        <span className="figures text-sm text-ink">{f.code}</span>
        <span className="micro ms-3 text-ink-dim">{f.airline}</span>
      </td>
      <td className="px-4 py-3.5 md:px-6">
        <span className="text-sm text-ink">{f.city}</span>
        {f.iata ? <span className="micro ms-2 text-ink-dim">{f.iata}</span> : null}
      </td>
      <td className="figures px-4 py-3.5 text-sm text-ink-dim md:px-6">{stand}</td>
      <td className="px-4 py-3.5 md:px-6">
        <span className="micro inline-flex items-center gap-2 whitespace-nowrap text-ink-dim">
          <span
            aria-hidden="true"
            className="h-2 w-2 shrink-0 rounded-full"
            style={{ background: token.dot }}
          />
          {tr(token.key)}
        </span>
      </td>
    </tr>
  );
}
