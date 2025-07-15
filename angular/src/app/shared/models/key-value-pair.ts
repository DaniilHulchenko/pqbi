export class KeyValuePair<KeyType, ValueType>{
    key: KeyType;
    value: ValueType;
    description: string;

    constructor(key: KeyType, value: ValueType, description: string = null) {
        this.key = key;
        this.value = value;
        this.description = description ?? '';
    }
}
