import { Component, Input, Output, OnInit, EventEmitter, signal } from "@angular/core";
import { Connection, ConnectionType } from "../../models/connection.model";
import { AgGridModule } from "ag-grid-angular";
import {
  ColDef,
  GridReadyEvent,
  Module,
  themeQuartz,
  colorSchemeDark,
  GridApi,
  GetRowIdFunc,
  GetRowIdParams,
  SelectionChangedEvent,
  ClientSideRowModelModule,
  RowSelectionOptions,
  RowSelectionModule,
  ModuleRegistry,
  CellClickedEvent,
} from "ag-grid-community";
import { GridActionsComponent } from "./grid-actions/grid-actions.component";
import { ConnectionsGridService } from "./connections-grid.service";
import { ConnectionsGridAction } from "../../models/connections-grid-action.model";


ModuleRegistry.registerModules([RowSelectionModule]);

@Component({
  selector: "connections-grid",
  standalone: true,
  imports: [AgGridModule],
  templateUrl: "./connections-grid.component.html",
  styleUrl: "./connections-grid.component.scss",
})
export class ConnectionsGridComponent implements OnInit {
  @Input() connections: Connection[];
  @Input() connectionTypes: ConnectionType[];
  @Output() actionSelect = new EventEmitter<ConnectionsGridAction>();

  public gridApi: GridApi<Connection>;
  public selectedRow: Connection;
  public selectedRows: Connection[] = [];
  public modules: Module[] = [ClientSideRowModelModule];
  public gridTheme = themeQuartz.withPart(colorSchemeDark); //TODO: globalize theme related stuff

  components = {
    gridActionsComponent: GridActionsComponent,
  };

  constructor(private gridService: ConnectionsGridService) {}

  public getRowId: GetRowIdFunc = (row: GetRowIdParams<Connection>) => row.data.connectionID.toString();

  public rowSelection: RowSelectionOptions | "single" | "multiple" = {
    mode: "multiRow",
  };

  public columnDefs: ColDef[] = [
    {
      headerName: "Name",
      field: "name",
    },
    {
      headerName:"Type",
      field: "connectionType",
      valueGetter: params => params.data.connectionType?.name
    },
    {
      headerName: "Description",
      field: "description",
    },
    {
      headerName: "Base URL",
      field: "baseUrl",
    },
    {
      headerName: "API Key",
      field: "apiKey",
    },
    {
      headerName: "API Secret Key",
      field: "apiSecretKey",
    },
    {
      headerName: "Actions",
      field: "actions",
      cellRenderer: GridActionsComponent,
      onCellClicked: (event: CellClickedEvent) => {
        this.gridAction(event.data);
      }
    },
  ];
  public rowData: Connection[] = [];

  public defaultColDef = {
    sortable: true,
    filter: true,
    resizable: true,
  };

  public ngOnInit(): void {
    this.rowData = this.connections;



  }

  public onGridReady(grid: GridReadyEvent<Connection>) {
    this.gridApi = grid.api;
  }

  public onSelectionChanged(event: SelectionChangedEvent) {
    this.selectedRows = this.gridApi.getSelectedRows();

    if (this.selectedRows && this.selectedRows.length > 0) {
      this.selectedRow = this.selectedRows[0];
    }
  }

  public gridAction(rowData: Connection):void{
    let action = this.gridService.getCurrentAction();   
    let selectedAction: ConnectionsGridAction = {name: action, connection: rowData};
    this.actionSelect.emit(selectedAction);
  }
}
