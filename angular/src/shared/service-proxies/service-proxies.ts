    presentedName!: string | undefined;
    calcBase!: any | undefined;
    windowInterval!: any | undefined;
        if (_data) {
            this.presentedName = _data["presentedName"] ?? _data["PresentedName"];
            this.calcBase = _data["calcBase"] ?? _data["CalcBase"];
            this.windowInterval = _data["windowInterval"] ?? _data["WindowInterval"];
        }
        data["presentedName"] = this.presentedName;
        data["calcBase"] = this.calcBase;
        data["windowInterval"] = this.windowInterval;
    presentedName: string | undefined;
    calcBase: any | undefined;
    windowInterval: any | undefined;
