import { Component, OnInit, ViewChild } from '@angular/core';
import { ApiService } from 'src/app/services/api.service';
import { User } from 'src/app/models/user';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {
  user: User = new User();

  password: string;
  confirmedPassword: string;
  agreeTerms: boolean;

  @ViewChild('editForm')
  thisForm: HTMLFormElement;

  constructor(private apiService: ApiService) {
    (<any>window).onSubmit = this.onSubmit; // need this following as recaptcha is called on the global scope and doesn't know this class
    (<any>window).thisComponent = this;
  }

  ngOnInit() {}

  onSubmit(token) {
    const thisComponent: RegisterComponent = <RegisterComponent>((<any>window).thisComponent);
    thisComponent.user.custom = token;
    thisComponent.apiService
      .register(thisComponent.user)
      .then(value => {
        alert(`User ${thisComponent.user.emailAddress} created successfully`);
        thisComponent.thisForm.reset();
      })
      .catch(error => {
        alert(error.error);
      });
  }
}
