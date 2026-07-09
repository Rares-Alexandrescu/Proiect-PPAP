import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddCompanie } from './add-companie';

describe('AddCompanie', () => {
  let component: AddCompanie;
  let fixture: ComponentFixture<AddCompanie>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AddCompanie],
    }).compileComponents();

    fixture = TestBed.createComponent(AddCompanie);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
