import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-add-companie',
  imports: [],
  standalone: true,
  templateUrl: './add-companie.html',
  styleUrl: './add-companie.scss',
})
export class AddCompanie {}
