import { useSearchParams } from "react-router-dom";
import { useGetWorkouts } from "../_apis/get-workout";

export function useListWorkout() {
    const [searchParams] = useSearchParams();
    const request = parseParams(searchParams);
    const { data, refetch, isLoading } = useGetWorkouts(request);

    return { data, refetch, isLoading };
}

function parseParams(params: URLSearchParams) {
    const pageIndex = params.get("pageIndex");
    const pageSize = params.get("pageSize");

    return {
        pageIndex: pageIndex ? parseInt(pageIndex, 10) - 1 : 0,
        pageSize: pageSize ? parseInt(pageSize, 10) : 20,
        userId: params.get("userId") || undefined,
        exerciseId: params.get("exerciseId") || undefined,
        
    }
}