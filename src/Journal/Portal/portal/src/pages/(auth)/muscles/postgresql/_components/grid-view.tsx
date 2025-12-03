import { type ColDef, type ICellRendererParams, type ValueFormatterParams } from 'ag-grid-community';
import type { IMuscle } from '@/interfaces/models/muscle';

import { format } from 'date-fns';
import { useSearchParams } from 'react-router';
import { useEffect, useMemo, useState } from 'react';
import { useListMuscle } from '../_hooks/use-list-muscle';

import { BaseGrid } from '@/components/base/base-grid';
import { GridActions } from './grid/grid-actions';
import { DeleteConfirm } from './delete-confirm';

import { GridToolbar } from './grid/grid-toolbar';
import { GridPagination } from './grid/grid-pagination';
import { useUpdateUrlParams } from '@/lib/updateUrlParams';

const config = {
    pageIndex: 1,
    pageSize: 20,
}

export function GridView() {
    const [searchParams] = useSearchParams();
    const { update } = useUpdateUrlParams();
    const { data, refetch, isLoading } = useListMuscle();
    const [openDeleteDialog, setOpenDeleteDialog] = useState(false);
    const [selectedMuscleId, setSelectedMuscleId] = useState<string | null>(null);

    const colDefs = useMemo<ColDef<IMuscle>[]>(() => [
        {
            field: "id",
            minWidth: 200,
        },
        {
            field: "name",
            width: 150,
            flex: 1
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
            cellRendererParams: (params: ICellRendererParams) => ({
                onClick: () => {
                    setOpenDeleteDialog(true);
                    setSelectedMuscleId(params.data.id);
                },
                params,
            }),
        }
    ], []);

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
                <BaseGrid<IMuscle>
                    data={data?.items || []}
                    isLoading={isLoading}
                    colDefs={colDefs}
                    rowHeight={82}
                />
            </div>
            <GridPagination totalItems={data?.all || 0} />
            {
                selectedMuscleId && (
                    <DeleteConfirm open={openDeleteDialog} onOpenChange={setOpenDeleteDialog} muscleId={selectedMuscleId} />
                )
            }
        </div>
    )
}