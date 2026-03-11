import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { useCreateForm, type ExerciseSchemaType } from "../_hooks/use-create-form";
import { useCreateExercise } from "../_apis/create-exercise";
import { useNavigate } from "react-router";
import { toast } from "sonner";
import { Spinner } from "@/components/ui/spinner";

export function CreateForm() {
    const { form } = useCreateForm();
    const { mutate, isPending } = useCreateExercise();
    const navigate = useNavigate();

    function onSubmit(data: ExerciseSchemaType) {
        mutate(data, {
            onSuccess() {
                navigate(-1);
            },
            onError(error) {
                toast.error("Error creating exercise", {
                    description: error instanceof Error ? error.message : "Unknown error",
                });
            }
        });
    }

    function onReset() {
        form.reset();
    }

    return (
        <>
            {
                isPending && (
                    <div className="fixed inset-0 z-50 flex items-center justify-center">
                        <div className="absolute inset-0 bg-black opacity-10"></div>
                        <div className="relative flex flex-col items-center">
                            <Spinner className="size-8 text-primary" />
                            <span className="text-primary">
                                Submitting...
                            </span>
                        </div>
                    </div>
                )
            }

            <Form {...form} >
                <form
                    id="create"
                    onSubmit={form.handleSubmit(onSubmit)}
                    onReset={onReset}
                    className="space-y-4 grid grid-cols-2 gap-x-4"
                >
                    <FormField
                        control={form.control}
                        name="name"
                        render={({ field }) => (
                            <FormItem className="self-start">
                                <FormLabel>
                                    Name
                                    <span className="text-red-500">*</span>
                                </FormLabel>
                                <FormControl>
                                    <Input disabled={isPending} {...field} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                    <FormField
                        control={form.control}
                        name="type"
                        render={({ field }) => (
                            <FormItem className="self-start">
                                <FormLabel>
                                    Type
                                    <span className="text-red-500">*</span>
                                </FormLabel>
                                <FormControl>
                                    <Input disabled={isPending} {...field} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />

                    <FormField
                        control={form.control}
                        name="description"
                        render={({ field }) => (
                            <FormItem className="col-span-2">
                                <FormLabel>
                                    Description
                                    <span className="text-red-500">*</span>
                                </FormLabel>
                                <FormControl>
                                    <Textarea disabled={isPending} {...field} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                </form>
            </Form>
        </>
    )
}