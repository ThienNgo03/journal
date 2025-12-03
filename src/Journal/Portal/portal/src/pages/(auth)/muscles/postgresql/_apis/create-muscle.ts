import { portalClient } from "@/api-client";
import { useMutation } from "@tanstack/react-query";
import type { IBaseResponse } from "@/interfaces/response/base";

type CreateMusclePayload = {
    name: string;
};

async function postMuscles(payload: CreateMusclePayload): Promise<IBaseResponse> {
    const response = await portalClient.post("/muscle", payload);
    if (response.status !== 201) {
        throw new Error(`Error creating muscle: ${response.statusText}`);
    }
    return response.data;
}

export function useCreateMuscle() {
    return useMutation({
        mutationFn: postMuscles,
        mutationKey: ["create-muscle"],
    });
}