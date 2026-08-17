/**
 * Fallback BEY schedule.
 *
 * Used only when the airport site cannot be reached. The live board is
 * `/api/v1/flights`. The shape here matches that response so the UI does
 * not care which source filled it.
 */

export type FlightStatus =
  | 'on-time'
  | 'boarding'
  | 'delayed'
  | 'landed'
  | 'departed'
  | 'cancelled';

export type Flight = {
  /** IATA flight number. */
  code: string;
  airline: string;
  /** IATA code of the other end, when we know it. */
  iata?: string;
  city: string;
  country: string;
  /** Scheduled local time, 24h. */
  time: string;
  /** Minutes late. Zero unless the status is `delayed`. */
  delay: number;
  status: FlightStatus;
  terminal: string;
  gate: string;
};

export const DEPARTURES: Flight[] = [
  { code: 'ME 201', airline: 'Middle East Airlines', iata: 'CDG', city: 'Paris', country: 'France', time: '07:45', delay: 0, status: 'departed', terminal: 'A', gate: 'A4' },
  { code: 'ME 217', airline: 'Middle East Airlines', iata: 'LHR', city: 'London', country: 'United Kingdom', time: '08:20', delay: 0, status: 'boarding', terminal: 'A', gate: 'A7' },
  { code: 'TK 829', airline: 'Turkish Airlines', iata: 'IST', city: 'Istanbul', country: 'Türkiye', time: '09:05', delay: 0, status: 'on-time', terminal: 'B', gate: 'B2' },
  { code: 'ME 265', airline: 'Middle East Airlines', iata: 'DXB', city: 'Dubai', country: 'UAE', time: '09:40', delay: 25, status: 'delayed', terminal: 'A', gate: 'A9' },
  { code: 'QR 419', airline: 'Qatar Airways', iata: 'DOH', city: 'Doha', country: 'Qatar', time: '10:15', delay: 0, status: 'on-time', terminal: 'B', gate: 'B5' },
  { code: 'MS 707', airline: 'EgyptAir', iata: 'CAI', city: 'Cairo', country: 'Egypt', time: '11:30', delay: 0, status: 'on-time', terminal: 'B', gate: 'B1' },
  { code: 'ME 315', airline: 'Middle East Airlines', iata: 'FCO', city: 'Rome', country: 'Italy', time: '12:10', delay: 0, status: 'on-time', terminal: 'A', gate: 'A3' },
  { code: 'LH 1287', airline: 'Lufthansa', iata: 'FRA', city: 'Frankfurt', country: 'Germany', time: '13:25', delay: 0, status: 'on-time', terminal: 'B', gate: 'B8' },
  { code: 'ME 429', airline: 'Middle East Airlines', iata: 'AMM', city: 'Amman', country: 'Jordan', time: '14:00', delay: 15, status: 'delayed', terminal: 'A', gate: 'A2' },
  { code: 'AF 563', airline: 'Air France', iata: 'CDG', city: 'Paris', country: 'France', time: '15:45', delay: 0, status: 'on-time', terminal: 'B', gate: 'B4' },
  { code: 'EK 958', airline: 'Emirates', iata: 'DXB', city: 'Dubai', country: 'UAE', time: '16:30', delay: 0, status: 'on-time', terminal: 'B', gate: 'B6' },
  { code: 'ME 373', airline: 'Middle East Airlines', iata: 'ATH', city: 'Athens', country: 'Greece', time: '18:05', delay: 0, status: 'on-time', terminal: 'A', gate: 'A5' },
];

export const ARRIVALS: Flight[] = [
  { code: 'ME 202', airline: 'Middle East Airlines', iata: 'CDG', city: 'Paris', country: 'France', time: '06:55', delay: 0, status: 'landed', terminal: 'A', gate: 'A4' },
  { code: 'TK 828', airline: 'Turkish Airlines', iata: 'IST', city: 'Istanbul', country: 'Türkiye', time: '07:40', delay: 0, status: 'landed', terminal: 'B', gate: 'B2' },
  { code: 'QR 418', airline: 'Qatar Airways', iata: 'DOH', city: 'Doha', country: 'Qatar', time: '08:35', delay: 20, status: 'delayed', terminal: 'B', gate: 'B5' },
  { code: 'ME 218', airline: 'Middle East Airlines', iata: 'LHR', city: 'London', country: 'United Kingdom', time: '09:15', delay: 0, status: 'on-time', terminal: 'A', gate: 'A7' },
  { code: 'MS 708', airline: 'EgyptAir', iata: 'CAI', city: 'Cairo', country: 'Egypt', time: '10:50', delay: 0, status: 'on-time', terminal: 'B', gate: 'B1' },
  { code: 'LH 1286', airline: 'Lufthansa', iata: 'FRA', city: 'Frankfurt', country: 'Germany', time: '12:05', delay: 0, status: 'on-time', terminal: 'B', gate: 'B8' },
  { code: 'ME 266', airline: 'Middle East Airlines', iata: 'DXB', city: 'Dubai', country: 'UAE', time: '13:40', delay: 0, status: 'on-time', terminal: 'A', gate: 'A9' },
  { code: 'EK 957', airline: 'Emirates', iata: 'DXB', city: 'Dubai', country: 'UAE', time: '15:10', delay: 0, status: 'on-time', terminal: 'B', gate: 'B6' },
  { code: 'AF 562', airline: 'Air France', iata: 'CDG', city: 'Paris', country: 'France', time: '16:20', delay: 0, status: 'on-time', terminal: 'B', gate: 'B4' },
  { code: 'ME 430', airline: 'Middle East Airlines', iata: 'AMM', city: 'Amman', country: 'Jordan', time: '17:35', delay: 0, status: 'on-time', terminal: 'A', gate: 'A2' },
  { code: 'ME 374', airline: 'Middle East Airlines', iata: 'ATH', city: 'Athens', country: 'Greece', time: '19:50', delay: 0, status: 'on-time', terminal: 'A', gate: 'A5' },
  { code: 'ME 316', airline: 'Middle East Airlines', iata: 'FCO', city: 'Rome', country: 'Italy', time: '21:15', delay: 0, status: 'on-time', terminal: 'A', gate: 'A3' },
];

/** BEY facts, for the panel beside the board. */
export const AIRPORT = {
  name: 'Beirut–Rafic Hariri International',
  iata: 'BEY',
  icao: 'OLBA',
  distanceKm: 9,
  terminals: 2,
  runways: 3,
  elevationM: 26,
};
