import {
    Pagination,
    PaginationContent,
    PaginationItem,
    PaginationLink,
    PaginationNext,
    PaginationPrevious,
} from "@/components/ui/pagination"
import { useUpdateUrlParams } from "@/lib/updateUrlParams";
import { useMemo } from "react";
import { useSearchParams } from "react-router-dom";

type GetPaginationProps = {
    totalItems: number;
}

export function GridPagination({ totalItems }: GetPaginationProps) {
    const [searchParams] = useSearchParams();
    const { update } = useUpdateUrlParams();
    const pageSize = parseInt(searchParams.get("pageSize") || "20", 10);
    const currentPage = parseInt(searchParams.get("pageIndex") || "1", 10);

    const totalPages = useMemo(() => {
        return Math.ceil(totalItems / pageSize)
    }, [totalItems, pageSize]);

    const paginationItems = useMemo(() => {
        return Array.from({ length: totalPages }, (_, i) => (
            <PaginationItem key={i}>
                <PaginationLink
                    className="cursor-pointer"
                    onClick={() => update({
                        pageIndex: (i + 1).toString()
                    })}
                    isActive={currentPage === i + 1}
                >
                    {i + 1}
                </PaginationLink>
            </PaginationItem>
        ));
    }, [currentPage, totalPages, update]);

    return (
        <Pagination>
            <PaginationContent>
                <PaginationItem>
                    <PaginationPrevious
                        className="cursor-pointer"
                        onClick={() => update({ pageIndex: Math.max(currentPage - 1, 1).toString() })}
                    />
                </PaginationItem>
                {paginationItems}
                <PaginationItem>
                    <PaginationNext
                        className="cursor-pointer"
                        onClick={() => update({ pageIndex: Math.min(currentPage + 1, totalPages).toString() })}
                    />
                </PaginationItem>
            </PaginationContent>
        </Pagination >
    )
}