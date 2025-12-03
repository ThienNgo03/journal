import { useParams } from "react-router"
import { useGetMuscles } from "./_apis/get-muscle";

export default function Page() {
    const { id } = useParams<{ id: string }>();
    const { data } = useGetMuscles({ ids: id });
    if (data?.items.length === 0) {
        return <div>Muscle not found</div>
    }

    return <div>{JSON.stringify(data?.items)}</div>
}