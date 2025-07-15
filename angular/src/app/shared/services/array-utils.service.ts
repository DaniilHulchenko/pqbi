export class ArrayUtils {
    static ensureArray<T>(value: T | T[]): T[] {
        if (value === null || value === undefined) {
            return [];
        }
        return Array.isArray(value) ? value : [value];
    }
}
