import type { IBaseModel } from "./base";

export interface IWorkout extends IBaseModel {
    userId: string;
    exerciseId: string;
}