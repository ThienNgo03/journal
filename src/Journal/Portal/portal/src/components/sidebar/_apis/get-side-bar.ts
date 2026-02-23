import { useQuery } from "@tanstack/react-query";
import { useEffect } from "react";
import { toast } from "sonner";
import { portalClient } from "@/api-client";
import { getErrorMessage } from "@/lib/getErrorMessage";

export interface SidebarDatabase {
    title: string;
    isPrimary: boolean;
    metaData: string;
}

export interface SidebarItem {
    title: string;
    url: string;
    databases: SidebarDatabase[];
}

async function getSidebar(): Promise<SidebarItem[]> {
    const response = await portalClient.get("/portal/side-bar");
    if (response.status !== 200) {
        throw new Error(`Error fetching sidebar: ${response.statusText}`);
    }
    return response.data;
}

export function useGetSidebar() {
    const { data, refetch, error, isError, isLoading, isFetching } = useQuery({
        queryKey: ["sidebar"],
        queryFn: getSidebar,
    })

    useEffect(() => {
        if (isError) {
            toast.error(`Error fetching sidebar`, {
                description: getErrorMessage(error),
            });
        }
    }, [isError, error]);

    return { data, refetch, isLoading: isLoading || isFetching };
}
