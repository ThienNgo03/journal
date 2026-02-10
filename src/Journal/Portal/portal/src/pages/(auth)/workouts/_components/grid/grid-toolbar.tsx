import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import debounce from "lodash.debounce";
import { Plus, RotateCcw } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router";
import { useUpdateUrlParams } from "@/lib/updateUrlParams";

type GridToolbarProps = {
    refetch: () => void;
}

export function GridToolbar({ refetch }: GridToolbarProps) {
    const navigate = useNavigate();
    const { update } = useUpdateUrlParams();
    const [searchValue, setSearchValue] = useState<string>("");

    const onSearchChange = (value: string) => {
        setSearchValue(value);
        debouncedSearchChange(value);
    }

    const debouncedSearchChange = useMemo(() => {
        return debounce((value: string) => {
            update({ searchTerm: value });
        }, 700);
    }, [update]);

    useEffect(() => {
        return () => {
            debouncedSearchChange.cancel();
        };
    }, [debouncedSearchChange]);

    return (
        <div className="flex justify-between items-center">
            <Input
                placeholder="Search muscles..."
                className="max-w-sm"
                value={searchValue}
                onChange={(e) => onSearchChange(e.target.value)}
            />
            <div className="flex gap-2">
                <Button variant="outline" onClick={() => refetch()}>
                    <RotateCcw className="h-4 w-4" />
                    Refresh
                </Button>
                <Button onClick={() => navigate("/muscles/create")}>
                    <Plus className="h-4 w-4" />
                    Create
                </Button>
            </div>
        </div>
    )
}