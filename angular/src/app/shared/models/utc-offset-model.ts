/**
 * UTC offset mode types
 */
export type UtcOffsetMode = 'timezone' | 'custom' | 'manual';

/**
 * UTC offset configuration model
 * This model represents the configuration for UTC offset settings
 */
export class UtcOffsetModel {
    /**
     * UTC offset mode: 'timezone' (use system timezone), 'custom' (select custom timezone), or 'manual' (set manual offset)
     */
    mode: UtcOffsetMode;

    /**
     * Custom time zone value (used when mode is 'custom')
     */
    customTimeZone: string;

    /**
     * Manual UTC offset in hours (used when mode is 'manual')
     */
    manualUtcOffset: number;

    constructor(
        mode: UtcOffsetMode = 'timezone',
        customTimeZone: string = '',
        manualUtcOffset: number = 0,
    ) {
        this.mode = mode;
        this.customTimeZone = customTimeZone;
        this.manualUtcOffset = manualUtcOffset;
    }

    /**
     * Creates UtcOffsetModel from JSON string
     * @param json JSON string representation of the model
     * @returns UtcOffsetModel instance or null if parsing fails
     */
    static fromJson(json: string): UtcOffsetModel | null {
        if (!json) {
            return new UtcOffsetModel();
        }

        try {
            const parsed = JSON.parse(json);
            if (parsed && (parsed.mode === 'timezone' || parsed.mode === 'custom' || parsed.mode === 'manual')) {
                return new UtcOffsetModel(
                    parsed.mode,
                    parsed.customTimeZone || '',
                    parsed.manualUtcOffset || 0,
                );
            }
        } catch (e) {
            // Invalid JSON, return default
        }

        return new UtcOffsetModel();
    }

    /**
     * Converts UtcOffsetModel to JSON string
     * @returns JSON string representation of the model
     */
    toJson(): string {
        return JSON.stringify({
            mode: this.mode,
            customTimeZone: this.customTimeZone,
            manualUtcOffset: this.manualUtcOffset,
        });
    }

    /**
     * Creates a copy of the model
     * @returns New UtcOffsetModel instance with the same values
     */
    clone(): UtcOffsetModel {
        return new UtcOffsetModel(this.mode, this.customTimeZone, this.manualUtcOffset);
    }
}

