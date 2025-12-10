import { type ColDef, type ValueFormatterParams } from 'ag-grid-community';

import { useSearchParams } from 'react-router';
import { useEffect } from 'react';

import { BaseGrid } from '@/components/base/base-grid';
import { GridActions } from './grid/grid-actions';

import { format } from 'date-fns';
import { GridToolbar } from './grid/grid-toolbar';
import { GridPagination } from './grid/grid-pagination';
import { useUpdateUrlParams } from '@/lib/updateUrlParams';
import type { IMuscle } from '@/interfaces/models/muscle';
import { useListWorkout } from '../../_hooks/use-list-workout';
import type { IWorkout } from '@/interfaces/models/workout';

const colDefs: ColDef<IWorkout>[] = [
    {
        field: "id",
        minWidth: 200,
    },
    {
        field: "createdDate",
        width: 150,
        valueFormatter: (params: ValueFormatterParams) => {
            return format(params.value, "dd/MM/yyyy");
        },
    },
    {
        field: "lastUpdated",
        width: 150,
        valueFormatter: (params: ValueFormatterParams) => {
            return format(params.value, "dd/MM/yyyy");
        },
    },
    {
        field: "createdById",
        minWidth: 200,
    },
    {
        field: "updatedById",
        minWidth: 200,
    },
    {
        colId: "actions",
        headerName: "Actions",
        width: 100,
        cellRenderer: GridActions,
    }
]

const config = {
    pageIndex: 1,
    pageSize: 20,
}

export function GridView() {
    const [searchParams] = useSearchParams();
    const { update } = useUpdateUrlParams();
    const { data, refetch, isLoading } = useListWorkout();

    useEffect(() => {
        const pageIndex = searchParams.get("pageIndex");
        const pageSize = searchParams.get("pageSize");
        update({
            pageIndex: pageIndex ?? config.pageIndex.toString(),
            pageSize: pageSize ?? config.pageSize.toString()
        });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return (
        <div>
            <GridToolbar refetch={refetch} />
            <div className="h-[calc(100vh-210px)] my-2">
                <BaseGrid<IWorkout>
                    data={data?.items || []}
                    isLoading={isLoading}
                    colDefs={colDefs}
                    rowHeight={80}
                />
            </div>
            <GridPagination totalItems={data?.all || 0} />
        </div>
    )
}