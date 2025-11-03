export interface GaugeStyle {
    style: GaugeStyleEnum;
    orientation: GaugeStyleOrientationEnum;
    arcWidth: number | null;
    arcAngle: GaugeStyleArcAngleEnum;
    startAngle: number | null;
    endAngle: number | null;
    trackWidth: number | null;
    isInvertScale: boolean;
}

export enum GaugeStyleEnum {
    Circle = 1,
    Linear = 2,
}

export enum GaugeStyleOrientationEnum {
    Horizontal = 1,
    Vertical = 2
}

export enum GaugeStyleArcAngleEnum {
    TopHalf = 1,
    FullCircle = 2,
    BottomHalf = 3,
    LeftHalf = 4,
    RightHalf = 5,
    ThreeQuarters = 6,
    Quarter = 7,
    Custom = 8
}