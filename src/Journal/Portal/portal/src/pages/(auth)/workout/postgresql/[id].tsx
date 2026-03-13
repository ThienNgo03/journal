import { useParams } from "react-router"
import { useGetWorkouts } from "./_apis/get-workout";

export default function Page() {
    const { id } = useParams<{ id: string }>();
    const { data } = useGetWorkouts({ ids: id });
    if (data?.items.length === 0) {
        return <div>Workout not found</div>
    }

    return <div>{JSON.stringify(data?.items)}</div>
}