import { Component, Input, OnInit, AfterViewInit } from "@angular/core";
import { Connection, ConnectionType } from "../models/connection.model";
import { ConnectionsService } from "../services/connections.service";
import { FormBuilder, FormGroup, FormControl, ReactiveFormsModule, FormsModule } from "@angular/forms";
import { CommonModule } from "@angular/common";

@Component({
  selector: "connection",
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, CommonModule],
  templateUrl: "./connection.component.html",
  styleUrl: "./connection.component.scss",
})
export class ConnectionComponent implements OnInit, AfterViewInit {
  @Input() connectionTypes: ConnectionType[] = [];
  public connectionDetailsForm: FormGroup;

  constructor(private fb: FormBuilder, private connectionsService: ConnectionsService) {
    this.connectionDetailsForm = this.fb.group({
      connectionID: [""],
      connectionTypeID: [""],
      name: [""],
      description: [""],
      baseUrl: [""],
      apiKey: [""],
      apiSecretKey: [""],
    });
  }

  ngOnInit(): void {
    console.log("connection.component onInit():::");
    console.log(this.connectionsService.getSelectedConnection());

    this.connectionDetailsForm.patchValue(this.connectionsService.getSelectedConnection());
    console.log('connection types:::');
    console.log(this.connectionTypes);
  }

  ngAfterViewInit(): void {}

  onConnectionTypeChange(event: any): void {

  }
}
