import { ChevronDown, InfoIcon, LockIcon, Table2 } from "lucide-react"

import { Lineicons } from "@lineiconshq/react-lineicons";
import { PostgresqlOutlined, MongodbOutlined, RadisOutlined, MysqlOutlined, Database2Outlined } from "@lineiconshq/free-icons";

import {
    Sidebar,
    SidebarContent,
    SidebarFooter,
    SidebarGroup,
    SidebarGroupContent,
    SidebarGroupLabel,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
    SidebarMenuSub,
    SidebarMenuSubItem,
} from "@/components/ui/sidebar"
import { ThemeToggle } from "./theme-toggle"
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "./ui/collapsible"

const items = [
    {
        title: "Exercises",
        url: "/exercises",
        databases: [
            { title: "PostgreSQL", isPrimary: true, metadata: "43000 rows" },
            { title: "Redis", isPrimary: false, metadata: "12000 keys" },
            { title: "MySQL", isPrimary: false, metadata: "25000 rows" },
        ]
    },
    {
        title: "Muscles",
        url: "/muscles",
        databases: [
            { title: "PostgreSQL", isPrimary: true, metadata: "3000 rows" },
            { title: "MongoDB", isPrimary: false, metadata: "2000 keys" },
        ]
    },
    {
        title: "Workout",
        url: "/workout",
        databases: [
            { title: "MySQL", isPrimary: true, metadata: "100 rows" },
            { title: "MongoDB", isPrimary: false, metadata: "100 keys" },
        ]
    }
]

const dbIcons: Record<string, React.ReactNode> = {
    postgresql: <Lineicons icon={PostgresqlOutlined} className="w-4 h-4 text-blue-500 stroke-1" />,
    mongodb: <Lineicons icon={MongodbOutlined} className="w-4 h-4 text-green-500 stroke-1" />,
    redis: <Lineicons icon={RadisOutlined} className="w-4 h-4 text-red-500 stroke-1" />,
    mysql: <Lineicons icon={MysqlOutlined} className="w-4 h-4 text-black-500 stroke-1" />,
    fallback: <Lineicons icon={Database2Outlined} className="w-4 h-4 text-gray-500 stroke-1" />,

};

export function AppNavigation() {
    return (
        <Sidebar variant="floating" collapsible="icon" className="bg-background">
            <SidebarContent className="bg-background rounded-[8px]">
                <SidebarGroup>
                    <SidebarGroupLabel className="flex justify-between">
                        <div>
                            <p className="text-sm font-semibold text-foreground">Table</p>
                            <p className="text-muted-foreground">By database</p>
                        </div>
                        <ThemeToggle />
                    </SidebarGroupLabel>
                    <SidebarGroupContent className="mt-6">

                        {
                            items.map((item, idx) => (
                                <Collapsible key={idx} defaultOpen className="group/collapsible">
                                    <SidebarMenuItem>
                                        <CollapsibleTrigger asChild>
                                            <SidebarMenuButton className="h-12">
                                                <Table2 className="h-4 w-4" />
                                                <div>
                                                    <span className="text-sm font-semibold text-foreground">{item.title}</span>
                                                    <br />
                                                    <span className="text-xs text-muted-foreground">{item.databases.length} implementation{item.databases.length !== 1 ? 's' : ''}</span>
                                                </div>
                                                <ChevronDown className="ml-auto h-4 w-4 transition-transform duration-200 group-data-[state=open]/collapsible:rotate-180" />
                                            </SidebarMenuButton>
                                        </CollapsibleTrigger>
                                        <CollapsibleContent>
                                            {
                                                item.databases.map((db, dbIdx) => {
                                                    const icon = dbIcons[db.title.toLowerCase()] ?? dbIcons['fallback'];
                                                    return (
                                                        <SidebarMenuSub key={dbIdx} className="mr-0 pr-0">
                                                            <SidebarMenuSubItem>
                                                                <SidebarMenuButton asChild className="h-10 text-xs">
                                                                    <a href={`${item.url}/${db.title.toLowerCase()}`}>
                                                                        {icon}
                                                                        <div>
                                                                            <span className="text-sm font-semibold text-foreground">{db.title}</span>
                                                                            <br />
                                                                            <span className="text-muted-foreground">{db.metadata}</span>
                                                                            <span className="px-2">●</span>
                                                                            {db.isPrimary ? <span className="text-green-400">PRIMARY</span> : <span className="text-blue-400">COPY</span>}
                                                                        </div>
                                                                    </a>
                                                                </SidebarMenuButton>
                                                            </SidebarMenuSubItem>
                                                        </SidebarMenuSub>
                                                    )
                                                }
                                                )
                                            }

                                        </CollapsibleContent>
                                    </SidebarMenuItem>
                                </Collapsible>
                            ))
                        }
                    </SidebarGroupContent>
                </SidebarGroup>
            </SidebarContent>
            <SidebarFooter className="bg-background rounded-[8px]">
                <SidebarMenu>
                    <SidebarMenuItem>
                        <SidebarMenuButton asChild>
                            <a href={"/about"}>
                                <InfoIcon />
                                <span>About</span>
                            </a>
                        </SidebarMenuButton>
                        <SidebarMenuButton variant={"outline"} asChild>
                            <a href={"/sign-in"}>
                                <LockIcon />
                                <span>Sign In</span>
                            </a>
                        </SidebarMenuButton>
                    </SidebarMenuItem>
                </SidebarMenu>
                <div className="text-center text-xs text-gray-500">
                    &copy; 2024 Journal Microservice
                </div>
            </SidebarFooter>
        </Sidebar >
    )
}