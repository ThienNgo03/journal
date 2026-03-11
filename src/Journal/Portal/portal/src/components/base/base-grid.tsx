import { AgGridReact } from "ag-grid-react";
import { AllCommunityModule, ModuleRegistry, themeQuartz, type ColDef, type RowSelectionOptions } from 'ag-grid-community';
import { NoRowOverlay } from "./grid/no-row-overlay";
ModuleRegistry.registerModules([AllCommunityModule]);

type BaseGridProps<T> = {
    data: T[];
    isLoading: boolean;
    colDefs: ColDef<T>[];
    rowHeight?: number;
}

export function BaseGrid<T>({ data, isLoading, colDefs, rowHeight }: BaseGridProps<T>) {

    const defaultColDef: ColDef<T> = {
        filter: false,
        editable: false,
        lockVisible: true,
    }

    const rowSelection: RowSelectionOptions = {
        mode: "multiRow",
        headerCheckbox: true,
    };

    return (
        <AgGridReact<T>
            theme={gridTheme}
            rowHeight={rowHeight}
            rowData={data}
            defaultColDef={defaultColDef}
            columnDefs={colDefs}
            loading={isLoading}
            rowSelection={rowSelection}
            noRowsOverlayComponent={NoRowOverlay}
        />
    )
}

const gridTheme = themeQuartz.withParams({
    fontFamily: 'var(--default-font-family)',
    backgroundColor: 'var(--background)',
    textColor: 'var(--foreground)',
    menuTextColor: 'var(--foreground)',
    borderColor: 'var(--input)',
    accentColor: 'var(--ring)',
    checkboxUncheckedBorderColor: 'var(--ring)',
    headerBackgroundColor: 'var(--muted)',
    headerColumnResizeHandleColor: 'oklch(0.708 0 0 / 50%)',
    headerColumnResizeHandleWidth: 1,
    headerColumnResizeHandleHeight: '70%',
    headerColumnBorder: false,
    columnBorder: false,
    borderRadius: 0,
    wrapperBorderRadius: 0,
});