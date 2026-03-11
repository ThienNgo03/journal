import { SidebarProvider, SidebarTrigger } from "./components/ui/sidebar";
import { AppNavigation } from "./components/sidebar/navigation";
import { Card } from "./components/ui/card";

export function Layouts({ children }: { children: React.ReactNode }) {
    return (
        <SidebarProvider defaultOpen={true}>
            <AppNavigation />
            <div className="flex flex-col w-full p-2 pl-0">
                <SidebarTrigger />
                <Card className="p-4 h-full shadow-lg bg-background"> 
                    {children}
                </Card>
            </div>
        </SidebarProvider>
    )
}