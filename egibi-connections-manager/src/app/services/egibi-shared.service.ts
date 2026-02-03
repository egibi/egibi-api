import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class EgibiSharedService {

  modalContent = signal<any>(null);

  constructor() { }

  setModalContent(content:any){
    this.modalContent.update(() => content);
  }

  getModalContent():any{
    return this.modalContent();
  }

}
