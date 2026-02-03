import { Injectable, signal } from "@angular/core";
import { Connection } from "../../models/connection.model";
@Injectable({
  providedIn: "root",
})
export class ConnectionsGridService {
  currentAction = signal<string>("");

  constructor() {}

  setCurrentAction(action: string) {
    this.currentAction.update(() => action);
  }

  getCurrentAction(): string {
    return this.currentAction();
  }

  setCurrentGridRows(connections: Connection[]) {}

  getCurrentGridRows(): Connection[] {
    return [];
  }
}
