import { toast } from "sonner";
import { useState } from "react";
import { useDeleteMuscle } from "../_apis/delete-muscle";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Copy } from "lucide-react";

type DeleteConfirmProps = {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    muscleId: string;
}

export function DeleteConfirm({ open, onOpenChange, muscleId }: DeleteConfirmProps) {
    const [confirmId, setConfirmId] = useState("");
    const { mutate, } = useDeleteMuscle();

    function onCopyClick() {
        navigator.clipboard.writeText(muscleId);
    }

    function onDialogOpenChange(open: boolean) {
        if (!open) {
            setConfirmId("");
        }
        onOpenChange(open);
    }

    function onDelete() {
        mutate(
            { muscleId, },
            {
                onSuccess() {
                    toast.success("Muscle deleted successfully", {
                        description: "Muscle deleted successfully",
                    });
                },
                onError(error) {
                    toast.error("Error deleting muscle", {
                        description: error instanceof Error ? error.message : "Unknown error",
                    });
                }
            }
        );
    }

    return (
        <AlertDialog open={open} onOpenChange={onDialogOpenChange} key={muscleId}>
            <AlertDialogContent>
                <AlertDialogHeader>
                    <AlertDialogTitle>Confirm Deletion</AlertDialogTitle>
                    <AlertDialogDescription asChild>
                        <div>
                            This action is irreversible.
                            <br />
                            The entity with ID
                            <span className="font-bold"> {muscleId}
                                <Button variant={"ghost"} size={"icon"} className="w-6 h-6" onClick={onCopyClick}>
                                    <Copy className="h-4 w-4" />
                                </Button>
                            </span>
                            will be permanently deleted from our servers.
                            <br />

                            To proceed, please re-enter the <span className="font-bold">ID</span> below:
                            <br />
                            <Input value={confirmId} onChange={(e) => setConfirmId(e.target.value)} />
                        </div>
                    </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                    <AlertDialogCancel>No</AlertDialogCancel>
                    <AlertDialogAction
                        disabled={confirmId !== muscleId}
                        onClick={onDelete}
                    >
                        Yes
                    </AlertDialogAction>
                </AlertDialogFooter>
            </AlertDialogContent>
        </AlertDialog>
    )
}