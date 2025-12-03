import { useSearchParams } from "react-router-dom";
import { useGetMuscles } from "../_apis/get-muscle";

export function useListMuscle() {
    const [searchParams] = useSearchParams();
    const request = parseParams(searchParams);
    const { data, refetch, isLoading } = useGetMuscles(request);

    return { data, refetch, isLoading };
}

function parseParams(params: URLSearchParams) {
    const pageIndex = params.get("pageIndex");
    const pageSize = params.get("pageSize");

    return {
        pageIndex: pageIndex ? parseInt(pageIndex, 10) - 1 : 0,
        pageSize: pageSize ? parseInt(pageSize, 10) : 20,
        name: params.get("searchTerm") || undefined,
    }
}