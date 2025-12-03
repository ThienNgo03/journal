import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";

const schema = z.object({
    name: z.string().min(1, "Name is required")
})

export type MuscleSchemaType = z.infer<typeof schema>;

export function useCreateForm() {
    const initialValues: MuscleSchemaType = {
        name: ""
    };

    const form = useForm<MuscleSchemaType>({
        resolver: zodResolver(schema),
        defaultValues: initialValues,
    });

    return { form };
}   