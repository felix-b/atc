import { World } from './proto';

declare global {
    interface ObjectConstructor {
      fromEntries(xs: [string|number|symbol, any][]): object
    }
}

