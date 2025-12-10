import { portalClient } from "@/api-client";
import { useMutation } from "@tanstack/react-query";
import type { IBaseResponse } from "@/interfaces/response/base";

type CreateWorkoutPayload = {
    exerciseId: string;
    userId: string;
    dateOfWeek: string;
};

async function postWorkouts(payload: CreateWorkoutPayload): Promise<IBaseResponse> {
    const response = await portalClient.post("/workouts", payload);
    if (response.status !== 201) {
        throw new Error(`Error creating workout: ${response.statusText}`);
    }
    return response.data;
}

export function useCreateWorkout() {
    return useMutation({
        mutationFn: postWorkouts,
        mutationKey: ["create-workout"],
    });
}