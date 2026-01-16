/**
 * Date and time display format mode types
 */
export type DateTimeDisplayFormatMode = 'auto' | 'custom' | 'manual';

/**
 * Date and time display format configuration model
 * This model represents the configuration for date and time display format settings
 */
export class DateTimeDisplayFormatModel {
    /**
     * Display format mode: 'auto' (use system culture), 'custom' (select custom culture), or 'manual' (set manual formats)
     */
    mode: DateTimeDisplayFormatMode;

    /**
     * Custom culture value (used when mode is 'custom')
     */
    customCulture: string;

    /**
     * Manual date format (used when mode is 'manual')
     */
    manualDateFormat: string;

    /**
     * Manual time format (used when mode is 'manual')
     */
    manualTimeFormat: string;

    constructor(
        mode: DateTimeDisplayFormatMode = 'auto',
        customCulture: string = '',
        manualDateFormat: string = '',
        manualTimeFormat: string = '',
    ) {
        this.mode = mode;
        this.customCulture = customCulture;
        this.manualDateFormat = manualDateFormat;
        this.manualTimeFormat = manualTimeFormat;
    }

    /**
     * Creates DateTimeDisplayFormatModel from JSON string
     * @param json JSON string representation of the model
     * @returns DateTimeDisplayFormatModel instance or null if parsing fails
     */
    static fromJson(json: string): DateTimeDisplayFormatModel | null {
        if (!json) {
            return new DateTimeDisplayFormatModel();
        }

        try {
            const parsed = JSON.parse(json);
            if (parsed && (parsed.mode === 'auto' || parsed.mode === 'custom' || parsed.mode === 'manual')) {
                return new DateTimeDisplayFormatModel(
                    parsed.mode,
                    parsed.customCulture || '',
                    parsed.manualDateFormat || '',
                    parsed.manualTimeFormat || '',
                );
            }
        } catch (e) {
            // Invalid JSON, return default
        }

        return new DateTimeDisplayFormatModel();
    }

    /**
     * Converts DateTimeDisplayFormatModel to JSON string
     * @returns JSON string representation of the model
     */
    toJson(): string {
        return JSON.stringify({
            mode: this.mode,
            customCulture: this.customCulture,
            manualDateFormat: this.manualDateFormat,
            manualTimeFormat: this.manualTimeFormat,
        });
    }

    /**
     * Creates a copy of the model
     * @returns New DateTimeDisplayFormatModel instance with the same values
     */
    clone(): DateTimeDisplayFormatModel {
        return new DateTimeDisplayFormatModel(
            this.mode,
            this.customCulture,
            this.manualDateFormat,
            this.manualTimeFormat,
        );
    }
}

