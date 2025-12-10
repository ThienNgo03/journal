import { useQuery } from "@tanstack/react-query";
import { useEffect } from "react";
import { toast } from "sonner";
import { portalClient } from "@/api-client";
import type { IBaseRequest } from "@/interfaces/request/base";
import type { IBaseGetResponse } from "@/interfaces/response/base-get";
import { getErrorMessage } from "@/lib/getErrorMessage";
import type { IWorkout } from "@/interfaces/models/workout";

export interface GetWorkoutRequest extends IBaseRequest<IWorkout> {
    userId?: string;
    exerciseId?: string;
}

async function getWorkouts(params: GetWorkoutRequest): Promise<IBaseGetResponse<IWorkout>> {
    const response = await portalClient.get("/workouts", { params });
    if (response.status !== 200) {
        throw new Error(`Error fetching workouts: ${response.statusText}`);
    }
    return response.data;
}

export function useGetWorkouts(params: GetWorkoutRequest) {
    const { data, refetch, error, isError, isLoading, isFetching } = useQuery({
        queryKey: ["workouts", params],
        queryFn: () => getWorkouts(params),
    })

    useEffect(() => {
        if (isError) {
            toast.error(`Error fetching exercises`, {
                description: getErrorMessage(error),
            });
        }
    }, [isError, error]);

    return { data, refetch, isLoading: isLoading || isFetching };
}