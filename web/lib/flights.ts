import { api } from '@/lib/api';
import {
  AIRPORT,
  ARRIVALS as FALLBACK_ARRIVALS,
  DEPARTURES as FALLBACK_DEPARTURES,
  type Flight,
} from '@/data/flights';

export { AIRPORT, type Flight, type FlightStatus } from '@/data/flights';
export { ARRIVALS as FALLBACK_ARRIVALS, DEPARTURES as FALLBACK_DEPARTURES } from '@/data/flights';

export type FlightBoard = {
  live: boolean;
  retrievedAt: string;
  arrivals: Flight[];
  departures: Flight[];
};

const EMPTY: FlightBoard = {
  live: false,
  retrievedAt: new Date(0).toISOString(),
  arrivals: FALLBACK_ARRIVALS,
  departures: FALLBACK_DEPARTURES,
};

export async function fetchFlights(): Promise<FlightBoard> {
  try {
    const board = await api<FlightBoard>('/api/v1/flights');
    if (!board.arrivals?.length && !board.departures?.length) {
      return EMPTY;
    }
    return board;
  } catch {
    return EMPTY;
  }
}
