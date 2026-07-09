import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

import { environment } from '../../../../environments/environment'; 

@Component({
  selector: 'app-edit-companie',
  imports: [],
  standalone: true,
  templateUrl: './edit-companie.html',
  styleUrl: './edit-companie.scss',
})
export class EditCompanie {}
