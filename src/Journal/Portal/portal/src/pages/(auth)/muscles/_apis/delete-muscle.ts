import { portalClient } from "@/api-client";
import { useMutation } from "@tanstack/react-query";

type DeleteMuscleQuery = {
    muscleId: string;
};

async function deleteMuscles({ muscleId }: DeleteMuscleQuery): Promise<void> {
    const response = await portalClient.delete("/muscles", { params: { id: muscleId } });
    if (response.status !== 204) {
        throw new Error(`Error deleting muscle: ${response.statusText}`);
    }
    return response.data;
}

export function useDeleteMuscle() {
    return useMutation({
        mutationFn: deleteMuscles,
        mutationKey: ["delete-muscle"],
    });
}