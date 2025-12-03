import { useNavigate } from "react-router";
import { EllipsisVertical } from "lucide-react";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuGroup, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import type { ICellRendererParams } from "ag-grid-community";

interface CustomCellRendererProps extends ICellRendererParams {
    onClick: () => void;
}

export function GridActions({ data, onClick }: CustomCellRendererProps) {
    const navigate = useNavigate();

    return (
        <DropdownMenu>
            <DropdownMenuTrigger asChild>
                <Button size={"icon"} variant="ghost" >
                    <EllipsisVertical className="h-4 w-4" />
                </Button>

            </DropdownMenuTrigger>
            <DropdownMenuContent className="w-32" align="start">
                <DropdownMenuLabel className="font-semibold">
                    Actions
                </DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuGroup>
                    <DropdownMenuItem onClick={() => navigator.clipboard.writeText(data.id)}>
                        Copy ID
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={() => navigate(`/muscles/${data.id}`)}>
                        Edit
                    </DropdownMenuItem>
                    <DropdownMenuItem className="group text-red-400" onClick={onClick}>
                        <span className="group-hover:text-red-500">Delete</span>
                    </DropdownMenuItem>
                </DropdownMenuGroup>
            </DropdownMenuContent>
        </DropdownMenu>
    )
}