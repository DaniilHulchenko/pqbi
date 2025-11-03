import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class CacheStorageService {
    private readonly CACHE_PREFIX = 'pqbi_cache_';

    set<T>(key: string, data: T): void {
        try {
            const serialized = JSON.stringify(data);
            localStorage.setItem(this.CACHE_PREFIX + key, serialized);
        } catch (error) {
            console.error('CacheStorageService: Failed to cache data', key, error);
        }
    }

    get<T>(key: string): T | null {
        try {
            const serialized = localStorage.getItem(this.CACHE_PREFIX + key);
            if (serialized === 'undefined' || serialized === 'null') {
                return null;
            }
            if (serialized) {
                return JSON.parse(serialized) as T;
            }
        } catch (error) {
            console.error('CacheStorageService: Failed to retrieve cached data', key, error);
        }
        return null;
    }

    has(key: string): boolean {
        return localStorage.getItem(this.CACHE_PREFIX + key) !== null;
    }

    remove(key: string): void {
        localStorage.removeItem(this.CACHE_PREFIX + key);
    }

    clear(): void {
        const keys = Object.keys(localStorage);
        keys.forEach(key => {
            if (key.startsWith(this.CACHE_PREFIX)) {
                localStorage.removeItem(key);
            }
        });
    }

    clearByPattern(pattern: string): void {
        const keys = Object.keys(localStorage);
        keys.forEach(key => {
            if (key.startsWith(this.CACHE_PREFIX + pattern)) {
                localStorage.removeItem(key);
            }
        });
    }
}

