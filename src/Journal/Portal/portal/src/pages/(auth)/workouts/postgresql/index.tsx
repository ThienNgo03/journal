import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { GridView } from "./_components/grid-view"
export default function Page() {
    return (
        <div>
            <Tabs defaultValue="data">
                <TabsList>
                    <TabsTrigger value="data">Data</TabsTrigger>
                    <TabsTrigger value="schema">Schema</TabsTrigger>
                </TabsList>

                <TabsContent value="data">
                    <GridView />
                </TabsContent>
                <TabsContent value="schema">Data Schema</TabsContent>
            </Tabs>
        </div>
    )
}