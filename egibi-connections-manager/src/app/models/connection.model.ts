export class Connection {
    connectionID: string
    connectionTypeID: string
    name: string
    baseUrl: string
    apiKey: string
    apiSecretKey: string    
    description: string
} 

export class ConnectionType{
    connectionTypeID: number;
    name: string;
    description: string;
}