import { Button } from "@/components/ui/button";
import { CreateFrom } from "./_components/create-from";

export default function Page() {
    return (
        <div>
            <div className="flex flex-row justify-between items-center">
                <h2 className="text-xl font-bold">Create Muscle Page</h2>
                <div>
                    <Button form="create" type="reset" variant="outline">
                        Reset
                    </Button>
                    <Button form="create" type="submit" className="ml-2">
                        Submit
                    </Button>
                </div>
            </div>
            <CreateFrom />
        </div>
    )
}