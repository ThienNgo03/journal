import { useQuery } from "@tanstack/react-query";
import { useEffect } from "react";
import { toast } from "sonner";
import { portalClient } from "@/api-client";
import type { IBaseRequest } from "@/interfaces/request/base";
import type { IBaseGetResponse } from "@/interfaces/response/base-get";
import { getErrorMessage } from "@/lib/getErrorMessage";
import type { IMuscle } from "@/interfaces/models/muscle";

export interface GetMuscleRequest extends IBaseRequest<IMuscle> {
    name?: string;
}

async function getMuscles(params: GetMuscleRequest): Promise<IBaseGetResponse<IMuscle>> {
    const response = await portalClient.get("/muscles", { params });
    if (response.status !== 200) {
        throw new Error(`Error fetching muscles: ${response.statusText}`);
    }
    return response.data;
}

export function useGetMuscles(params: GetMuscleRequest) {
    const { data, refetch, error, isError, isLoading, isFetching } = useQuery({
        queryKey: ["muscles", params],
        queryFn: () => getMuscles(params),
    })

    useEffect(() => {
        if (isError) {
            toast.error(`Error fetching muscles`, {
                description: getErrorMessage(error),
            });
        }
    }, [isError, error]);

    return { data, refetch, isLoading: isLoading || isFetching };
}