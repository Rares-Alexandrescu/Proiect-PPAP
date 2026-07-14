import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditFurnizor } from './edit-furnizor';

describe('EditFurnizor', () => {
  let component: EditFurnizor;
  let fixture: ComponentFixture<EditFurnizor>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EditFurnizor],
    }).compileComponents();

    fixture = TestBed.createComponent(EditFurnizor);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
