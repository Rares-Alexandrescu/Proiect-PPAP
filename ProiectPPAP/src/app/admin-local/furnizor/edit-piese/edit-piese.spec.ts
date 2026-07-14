import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditPiese } from './edit-piese';

describe('EditPiese', () => {
  let component: EditPiese;
  let fixture: ComponentFixture<EditPiese>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EditPiese],
    }).compileComponents();

    fixture = TestBed.createComponent(EditPiese);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
