import { Component, EventEmitter, Input, Output } from "@angular/core";
import { ConnectionsGridService } from "../connections-grid.service";
import { GridOptions, GridOptionsService, ICellRendererParams } from "ag-grid-community";

@Component({
  selector: "grid-action-button",
  standalone: true,
  imports: [],
  templateUrl: "./grid-actions.component.html",
  styleUrl: "./grid-actions.component.scss",
})
export class GridActionsComponent {
  @Input() actions: any[] = [];
  
  cellValue: any;
  
  agInit(params:ICellRendererParams): void{
    this.cellValue = params;
  }

  refresh(params:ICellRendererParams): void{
    this.cellValue = params.value;
  }

  constructor(private connectionsGridService:ConnectionsGridService){
      
  }

  public actionButtonClicked(action: string) {
    this.connectionsGridService.setCurrentAction(action);
  }
}
