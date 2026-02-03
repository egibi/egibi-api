import { Injectable, signal } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { RequestResponse } from "../request-response.model";
import { Connection } from "../models/connection.model";

@Injectable({
  providedIn: "root",
})
export class ConnectionsService {
  private apiBaseUrl: string = "https://localhost:7182/Connections";
  public selectedConnectionComponent = signal<Connection>(new Connection());

  constructor(private http: HttpClient) {}

  setSelectedConnection(connection: Connection): void {
    this.selectedConnectionComponent.update(() => connection);
  }

  getSelectedConnection(): Connection {
    return this.selectedConnectionComponent();
  }

  //--------------------------------------------------------------------------------------------------------------
  // HTTP ACTIONS
  //______________________________________________________________________________________________________________

  public getConnections(): Observable<RequestResponse> {
    return this.http.get<RequestResponse>(`${this.apiBaseUrl}/get-connections`);
  }

  public getConnection(id: number): Observable<RequestResponse> {
    return this.http.get<RequestResponse>(`${this.apiBaseUrl}/get-connection` + "?id=" + id);
  }

  public getConnectionTypes(): Observable<RequestResponse> {
    return this.http.get<RequestResponse>(`${this.apiBaseUrl}/get-connection-types`);
  }

  public saveConnection(connection: Connection): Observable<RequestResponse> {
    return this.http.post<RequestResponse>(`${this.apiBaseUrl}/save-connection`, connection);
  }

  public deleteConnection(connection: Connection): Observable<RequestResponse> {
    return this.http.delete<RequestResponse>(`${this.apiBaseUrl}/delete-connection` + "?id=" + connection.connectionID);
  }
  //********************************************************************************************************* */
}
